using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models;
using MicroShift.Utils;

namespace MicroShift.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobsController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. THE JOB FEED (With Radius & Category Filter) ---
        public async Task<IActionResult> Index(int radius = 15, int? categoryId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.CurrentRadius = radius;

            // Load Categories for the filter dropdown
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Fetch open jobs
            var query = _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.Category)
                .Where(j => j.Status == "Open");

            // Filter by Category if selected
            if (categoryId.HasValue)
            {
                query = query.Where(j => j.CategoryId == categoryId.Value);
            }

            var allJobs = await query.ToListAsync();

            // TRACK REACH: Every time a job appears in a search/feed, increment ImpressionCount
            foreach (var j in allJobs)
            {
                j.ImpressionCount++;
            }
            await _context.SaveChangesAsync();

            // If user has no GPS data, show all jobs sorted by Newest & Emergency status
            if (user == null || user.Latitude == null || user.Longitude == null)
            {
                return View(allJobs.OrderByDescending(j => j.IsEmergency).ThenByDescending(j => j.CreatedAt));
            }

            var nearbyJobs = new List<Job>();

            foreach (var job in allJobs)
            {
                // Logic Fix: Latitude/Longitude are now standard doubles (no .HasValue needed)
                job.DistanceFromUser = GeoCalculator.GetDistanceInKm(
                    user.Latitude.Value, user.Longitude.Value,
                    job.Latitude, job.Longitude);

                if (job.DistanceFromUser <= radius)
                {
                    nearbyJobs.Add(job);
                }
            }

            // Sort: Emergency First, then by Distance
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
                // Calculate distance if user has GPS
                if (user.Latitude.HasValue && user.Longitude.HasValue)
                {
                    job.DistanceFromUser = GeoCalculator.GetDistanceInKm(
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
            // Pass categories to the view for the dropdown
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // --- 4. POST A JOB (Save to Database) ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Set initial metadata
            job.EmployerId = user.Id;
            job.CreatedAt = DateTime.UtcNow;
            job.Status = "Open";

            // Default Commission logic (Sprint 1)
            var category = await _context.Categories.FindAsync(job.CategoryId);
            job.FinalCommissionPercentage = category?.CategoryCommissionPercentage ?? 10.0;

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