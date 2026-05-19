using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models;
using MicroShift.Utils;
using MicroShift.Helpers;
using Microsoft.AspNetCore.Hosting;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace MicroShift.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public JobsController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment; // NEW
        }

        // --- 1. THE JOB FEED (With Live GPS, Radius & Category Filter) ---
        // Added userLat and userLon parameters to accept real-time device location from the client
        // --- 1. THE JOB FEED (With Advanced Filters) ---
        public async Task<IActionResult> Index(
            int radius = 15,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? jobType = null,
            string? shift = null,
            double? userLat = null,
            double? userLon = null)
        {
            var user = await _userManager.GetUserAsync(User);

            // Save current filter state to ViewBag so the UI form remembers what the user typed
            ViewBag.CurrentRadius = radius;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.JobType = jobType;
            ViewBag.Shift = shift;

            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Fetch open jobs
            var query = _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.Category)
                .Where(j => j.Status == "Open");

            // --- APPLY ADVANCED FILTERS ---
            if (categoryId.HasValue) query = query.Where(j => j.CategoryId == categoryId.Value);
            if (minPrice.HasValue) query = query.Where(j => j.PaymentAmount >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(j => j.PaymentAmount <= maxPrice.Value);
            if (!string.IsNullOrEmpty(jobType)) query = query.Where(j => j.JobType == jobType);
            if (!string.IsNullOrEmpty(shift)) query = query.Where(j => j.Shift == shift);

            var allJobs = await query.ToListAsync();

            foreach (var j in allJobs) j.ImpressionCount++;
            await _context.SaveChangesAsync();

            double? targetLat = userLat ?? user?.Latitude;
            double? targetLon = userLon ?? user?.Longitude;

            ViewBag.UserLat = targetLat;
            ViewBag.UserLon = targetLon;

            if (!targetLat.HasValue || !targetLon.HasValue)
            {
                return View(allJobs.OrderByDescending(j => j.IsEmergency).ThenByDescending(j => j.CreatedAt).ToList());
            }

            var nearbyJobs = new List<Job>();

            foreach (var job in allJobs)
            {
                job.DistanceFromUser = MicroShift.Helpers.GeoCalculator.GetDistanceInKm(
                    targetLat.Value, targetLon.Value,
                    job.Latitude, job.Longitude);

                // Ignore radius if it's an Online/Remote job!
                if (job.JobType == "Online" || job.JobType == "Remote" || job.DistanceFromUser <= radius)
                {
                    nearbyJobs.Add(job);
                }
            }

            var sortedJobs = nearbyJobs
                .OrderByDescending(j => j.IsEmergency)
                .ThenBy(j => j.DistanceFromUser)
                .ToList();

            return View(sortedJobs);
        }

        // --- 2. VIEW JOB DETAILS ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null) return NotFound();

            // TRACK CLICKS: Increment ViewCount when details are opened
            job.ViewCount++;
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.HasApplied = false;

            if (user != null)
            {
                if (user.Latitude.HasValue && user.Longitude.HasValue)
                {
                    // FIXED AMBIGUOUS REFERENCE
                    job.DistanceFromUser = MicroShift.Helpers.GeoCalculator.GetDistanceInKm(
                        user.Latitude.Value, user.Longitude.Value,
                        job.Latitude, job.Longitude);
                }

                if (await _userManager.IsInRoleAsync(user, "Worker"))
                {
                    var existingApp = await _context.JobApplications
                        .FirstOrDefaultAsync(a => a.JobId == id && a.WorkerId == user.Id);
                    if (existingApp != null) ViewBag.HasApplied = true;
                }
            }

            return View(job);
        }

        // --- 3. POST A JOB (Form Page) ---
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // --- 4. POST A JOB (Save to Database) ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Create(Job job, List<IFormFile> images)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            job.EmployerId = user.Id;
            job.CreatedAt = DateTime.UtcNow;
            job.Status = "Open";

            var category = await _context.Categories.FindAsync(job.CategoryId);
            job.FinalCommissionPercentage = category?.CategoryCommissionPercentage ?? 10.0;

            // --- IMAGE UPLOAD LOGIC ---
            // --- IMAGE UPLOAD & EXIF FORENSICS LOGIC ---
            if (images != null && images.Count > 0)
                // --- IMAGE UPLOAD & EXIF FORENSICS LOGIC ---
                if (images != null && images.Count > 0)
                {
                    // FIX 1: Explicitly tell C# to use System.IO for the folder directory
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "jobs");
                    System.IO.Directory.CreateDirectory(uploadsFolder);

                    int imageCount = 1;
                    foreach (var file in images.Take(5))
                    {
                        if (file.Length > 0)
                        {
                            // 1. EXIF EXTRACTION (We grab the metadata from the Primary Image)
                            if (imageCount == 1)
                            {
                                try
                                {
                                    using (var stream = file.OpenReadStream())
                                    {
                                        var directories = ImageMetadataReader.ReadMetadata(stream);

                                        // Extract Shutter Time
                                        var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                                        if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime captureTime))
                                        {
                                            job.ExifCaptureTime = captureTime;
                                        }

                                        // Extract Embedded Camera GPS
                                        var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
                                        var location = gpsDirectory?.GetGeoLocation();

                                        // FIX 2: Check .HasValue and use .Value for the struct
                                        if (location.HasValue && !location.Value.IsZero)
                                        {
                                            job.ExifLatitude = location.Value.Latitude;
                                            job.ExifLongitude = location.Value.Longitude;
                                        }
                                    }
                                }
                                catch
                                {
                                    // If the image has no EXIF data, we just ignore it and move on
                                }
                            }

                            // 2. SAVE THE FILE TO DISK
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            string dbPath = "/uploads/jobs/" + uniqueFileName;

                            if (imageCount == 1) job.JobImageUrl = dbPath;
                            else if (imageCount == 2) job.JobImageUrl2 = dbPath;
                            else if (imageCount == 3) job.JobImageUrl3 = dbPath;
                            else if (imageCount == 4) job.JobImageUrl4 = dbPath;
                            else if (imageCount == 5) job.JobImageUrl5 = dbPath;

                            imageCount++;
                        }
                    }
                }

            ModelState.Remove("EmployerId");
            ModelState.Remove("Employer");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", job.CategoryId);
            return View(job);
        }

        // --- 5. APPLY FOR A JOB ---
        [HttpPost]
        [Authorize(Roles = "Worker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existingApp = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobId == id && a.WorkerId == user.Id);

            if (existingApp == null)
            {
                var application = new JobApplication
                {
                    JobId = id,
                    WorkerId = user.Id,
                    Status = "Pending",
                    AppliedAt = DateTime.UtcNow
                };

                _context.JobApplications.Add(application);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}