using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR; // NEW
using MicroShift.Data;
using MicroShift.Models;
using MicroShift.Hubs; // NEW
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = MetadataExtractor.Directory;

namespace MicroShift.Controllers
{
    [Authorize] // Allow anyone logged in to enter the controller
    public class DashboardController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub; // NEW: The Real-Time Engine

        public DashboardController(
            MicroShiftDBContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> notificationHub) // Inject it here
        {
            _context = context;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        // --- EMPLOYER DASHBOARD ---
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myJobs = await _context.Jobs
                .Include(j => j.JobApplications)
                    .ThenInclude(a => a.Worker)
                .Where(j => j.EmployerId == user.Id)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(myJobs);
        }

        // --- EMPLOYER HIRE LOGIC ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HireWorker(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job == null) return NotFound();
            if (application.Job.EmployerId != user.Id) return Unauthorized();

            application.Status = "Accepted";
            application.Job.Status = "InProgress";

            var otherApplications = await _context.JobApplications
                .Where(a => a.JobId == application.JobId && a.Id != applicationId)
                .ToListAsync();

            foreach (var otherApp in otherApplications)
            {
                otherApp.Status = "Rejected";
            }

            // --- 🔔 NOTIFY WORKER THEY WERE HIRED ---
            var notif = new Notification
            {
                UserId = application.WorkerId,
                Title = "Application Accepted! 🎉",
                Message = $"You have been hired for '{application.Job.Title}'. Click here to view details.",
                NotificationType = "Application",
                ActionUrl = $"/Chat/Index?jobId={application.JobId}&receiverId={user.Id}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();

            // Push to screen instantly
            await _notificationHub.Clients.User(application.WorkerId)
                .SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            return RedirectToAction(nameof(Index));
        }

        // --- WORKER DASHBOARD ---
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> WorkerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myApplications = await _context.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.Employer)
                .Where(a => a.WorkerId == user.Id)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(myApplications);
        }

        // --- EMPLOYER: REJECT A WORKER ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWorker(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job?.EmployerId != user?.Id) return Unauthorized();

            application.Status = "Rejected";

            // --- 🔔 NOTIFY WORKER OF REJECTION ---
            var notif = new Notification
            {
                UserId = application.WorkerId,
                Title = "Application Update",
                Message = $"Your application for '{application.Job.Title}' was not selected.",
                NotificationType = "Application",
                ActionUrl = "/Dashboard/WorkerDashboard",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            await _notificationHub.Clients.User(application.WorkerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            return RedirectToAction(nameof(Index));
        }

        // --- EMPLOYER: DELETE A JOB ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == user!.Id);
            if (job == null) return NotFound();

            if (job.Status != "Open")
            {
                TempData["ErrorMessage"] = "Fraud Prevention: You cannot delete a job that is 'In Progress'. The worker must cancel, or payment must be processed.";
                return RedirectToAction(nameof(Index));
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // --- WORKER: WITHDRAW/CANCEL APPLICATION ---
        [HttpPost]
        [Authorize(Roles = "Worker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawApplication(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.WorkerId == user!.Id);

            if (application == null) return NotFound();

            if (application.Status == "Accepted" && application.Job?.Status == "InProgress")
            {
                application.Job.Status = "Open";

                // --- 🔔 NOTIFY EMPLOYER OF WITHDRAWAL ---
                var notif = new Notification
                {
                    UserId = application.Job.EmployerId,
                    Title = "Worker Withdrew ⚠️",
                    Message = $"The hired worker cancelled their task for '{application.Job.Title}'. The job has been reopened.",
                    NotificationType = "System",
                    ActionUrl = "/Dashboard/Index",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);
                await _notificationHub.Clients.User(application.Job.EmployerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);
            }

            _context.JobApplications.Remove(application);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(WorkerDashboard));
        }

        // --- WORKER: MARK JOB AS DONE & UPLOAD EVIDENCE ---
        [HttpPost]
        [Authorize(Roles = "Worker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsDone(int applicationId, IFormFile evidencePhoto)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.WorkerId == user!.Id);

            if (application == null || application.Job == null) return NotFound();
            if (application.Job.Status != "InProgress") return BadRequest();

            if (evidencePhoto != null && evidencePhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot/uploads/evidence");
                if (!System.IO.Directory.Exists(uploadsFolder)) System.IO.Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + evidencePhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await evidencePhoto.CopyToAsync(stream);
                }

                application.WorkerEvidenceUrl = "/uploads/evidence/" + uniqueFileName;

                try
                {
                    var directories = ImageMetadataReader.ReadMetadata(filePath);
                    var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                    if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTaken))
                    {
                        application.WorkerEvidenceDate = dateTaken;
                        if (dateTaken < application.Job.CreatedAt)
                        {
                            application.IsWorkerTimeFraudDetected = true;
                        }
                    }
                }
                catch (Exception) { }
            }

            application.Status = "ReviewPending";
            application.Job.Status = "ReviewPending";

            // --- 🔔 NOTIFY EMPLOYER JOB IS DONE ---
            var notif = new Notification
            {
                UserId = application.Job.EmployerId,
                Title = "Task Completed! ✅",
                Message = $"The worker marked '{application.Job.Title}' as done. Please review their evidence.",
                NotificationType = "Job",
                ActionUrl = "/Dashboard/Index",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            await _notificationHub.Clients.User(application.Job.EmployerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            return RedirectToAction(nameof(WorkerDashboard));
        }

        // --- EMPLOYER: APPROVE & FINALIZE JOB ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveJob(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job!.EmployerId == user!.Id);

            if (application == null || application.Job == null) return Unauthorized();

            application.Status = "Completed";
            application.Job.Status = "Completed";

            // --- 🔔 NOTIFY WORKER OF APPROVAL ---
            var notif = new Notification
            {
                UserId = application.WorkerId,
                Title = "Payment Approved! 💰",
                Message = $"The employer approved your work for '{application.Job.Title}'.",
                NotificationType = "Job",
                ActionUrl = "/Dashboard/WorkerDashboard",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            await _notificationHub.Clients.User(application.WorkerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            return RedirectToAction(nameof(Index));
        }

        // --- EMPLOYER: RAISE A DISPUTE ---
        [HttpPost]
        [Authorize(Roles = "Employer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisputeJob(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job!.EmployerId == user!.Id);

            if (application == null || application.Job == null) return Unauthorized();

            application.Status = "Disputed";
            application.Job.Status = "Disputed";
            application.EmployerDisputeDate = DateTime.UtcNow;

            // --- 🔔 NOTIFY WORKER OF DISPUTE ---
            var notif = new Notification
            {
                UserId = application.WorkerId,
                Title = "Job Disputed ⚠️",
                Message = $"The employer raised a dispute for '{application.Job.Title}'. Our team will review this.",
                NotificationType = "Dispute",
                ActionUrl = "/Dashboard/WorkerDashboard",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            await _notificationHub.Clients.User(application.WorkerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}