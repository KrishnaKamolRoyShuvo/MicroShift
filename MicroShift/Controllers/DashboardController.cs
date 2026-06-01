using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MicroShift.Data;
using MicroShift.Models;
using MicroShift.Hubs;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = MetadataExtractor.Directory;

namespace MicroShift.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public DashboardController(
            MicroShiftDBContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        // --- EMPLOYER DASHBOARD (Expanded Access) ---
        [Authorize(Roles = "Employer,Admin")]
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
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HireWorker(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (await _userManager.IsInRoleAsync(user, "Admin")) return Unauthorized("Admins cannot hire workers.");

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
            await _notificationHub.Clients.User(application.WorkerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

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
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWorker(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin")) return Unauthorized("Admins cannot reject workers.");

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job?.EmployerId != user?.Id) return Unauthorized();

            application.Status = "Rejected";

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
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin")) return Unauthorized("Admins cannot delete live jobs from this view.");

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
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveJob(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin")) return Unauthorized("Admins cannot process payments.");

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job!.EmployerId == user!.Id);

            if (application == null || application.Job == null) return Unauthorized();

            application.Status = "Completed";
            application.Job.Status = "Completed";

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

        // --- EMPLOYER: SUBMIT DISPUTE EVIDENCE ---
        [HttpPost]
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEmployerDispute(int applicationId, string disputeText, IFormFile disputePhoto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin")) return Unauthorized("Admins cannot raise disputes.");

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Job!.EmployerId == user!.Id);

            if (application == null || application.Job == null) return Unauthorized();

            if (disputePhoto == null || disputePhoto.Length == 0 || string.IsNullOrEmpty(disputeText))
            {
                TempData["ErrorMessage"] = "Both a text description and a photo are required to submit dispute evidence.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Process the Photo & Extract EXIF
            var uploadsFolder = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot/uploads/evidence");
            if (!System.IO.Directory.Exists(uploadsFolder)) System.IO.Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = "EMP_DISPUTE_" + Guid.NewGuid().ToString() + "_" + disputePhoto.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await disputePhoto.CopyToAsync(stream);
            }

            application.EmployerDisputeImageUrl = "/uploads/evidence/" + uniqueFileName;
            application.EmployerDisputeText = disputeText;

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTaken))
                {
                    application.EmployerDisputeExifTime = dateTaken;
                }
            }
            catch { /* Ignore if no EXIF */ }

            // 2. Set State and Identify Initiator
            if (string.IsNullOrEmpty(application.DisputeInitiator))
            {
                application.DisputeInitiator = "Employer";
            }

            application.Status = "Disputed";
            application.Job.Status = "Disputed";

            // 3. Notify the Worker
            var notif = new Notification
            {
                UserId = application.WorkerId,
                Title = "Dispute Evidence Required ⚠️",
                Message = $"The employer submitted dispute evidence for '{application.Job.Title}'. Please go to your dashboard to submit your counter-evidence.",
                NotificationType = "Dispute",
                ActionUrl = "/Dashboard/WorkerDashboard",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            await _notificationHub.Clients.User(application.WorkerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            TempData["SuccessMessage"] = "Your dispute evidence has been securely submitted to the Admin.";
            return RedirectToAction(nameof(Index));
        }

        // --- WORKER: SUBMIT DISPUTE EVIDENCE ---
        [HttpPost]
        [Authorize(Roles = "Worker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitWorkerDispute(int applicationId, string disputeText, IFormFile disputePhoto)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.WorkerId == user!.Id);

            if (application == null || application.Job == null) return Unauthorized();

            if (disputePhoto == null || disputePhoto.Length == 0 || string.IsNullOrEmpty(disputeText))
            {
                TempData["ErrorMessage"] = "Both a text description and a photo are required to submit dispute evidence.";
                return RedirectToAction(nameof(WorkerDashboard));
            }

            // 1. Process the Photo & Extract EXIF
            var uploadsFolder = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot/uploads/evidence");
            if (!System.IO.Directory.Exists(uploadsFolder)) System.IO.Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = "WRK_DISPUTE_" + Guid.NewGuid().ToString() + "_" + disputePhoto.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await disputePhoto.CopyToAsync(stream);
            }

            application.WorkerDisputeImageUrl = "/uploads/evidence/" + uniqueFileName;
            application.WorkerDisputeText = disputeText;

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTaken))
                {
                    application.WorkerDisputeExifTime = dateTaken;
                }
            }
            catch { /* Ignore if no EXIF */ }

            // 2. Set State and Identify Initiator
            if (string.IsNullOrEmpty(application.DisputeInitiator))
            {
                application.DisputeInitiator = "Worker";
            }

            application.Status = "Disputed";
            application.Job.Status = "Disputed";

            // 3. Notify the Employer
            var notif = new Notification
            {
                UserId = application.Job.EmployerId,
                Title = "Dispute Evidence Required ⚠️",
                Message = $"The worker submitted dispute evidence for '{application.Job.Title}'. Please go to your dashboard to submit your counter-evidence.",
                NotificationType = "Dispute",
                ActionUrl = "/Dashboard/Index",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            await _notificationHub.Clients.User(application.Job.EmployerId).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            TempData["SuccessMessage"] = "Your defense evidence has been securely submitted to the Admin.";
            return RedirectToAction(nameof(WorkerDashboard));
        }



        // --- WORKER WALLET ---
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> Wallet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myTransactions = await _context.Transactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentBalance = user.WalletBalance;
            return View(myTransactions);
        }


        // --- EMPLOYER BILLING & WALLET ---
        [Authorize(Roles = "Employer,Admin")]
        public async Task<IActionResult> EmployerWallet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Calculate Total Amount Paid (Sum of all Completed jobs)
            var completedJobs = await _context.JobApplications
                .Include(a => a.Job)
                .Where(a => a.Job.EmployerId == user.Id && a.Status == "Completed")
                .ToListAsync();

            decimal totalPaid = completedJobs.Sum(a => a.Job!.PaymentAmount);

            // 2. Fetch Pending Payments (ReviewPending jobs)
            var pendingApplications = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.Worker)
                .Where(a => a.Job!.EmployerId == user.Id && a.Status == "ReviewPending")
                .OrderBy(a => a.WorkerEvidenceDate)
                .ToListAsync();

            decimal totalPending = pendingApplications.Sum(a => a.Job!.PaymentAmount);

            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalPending = totalPending;

            return View(pendingApplications);
        }

    }
}