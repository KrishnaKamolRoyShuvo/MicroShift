using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MicroShift.Data;
using MicroShift.Models;
using MicroShift.Hubs;

namespace MicroShift.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public ReviewsController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        // GET: /Reviews/Create
        [HttpGet]
        public async Task<IActionResult> Create(int jobId, string revieweeId)
        {
            var reviewer = await _userManager.GetUserAsync(User);

            // Security check: Make sure they haven't already reviewed this person for this job
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.JobId == jobId && r.ReviewerId == reviewer!.Id);

            if (alreadyReviewed) return BadRequest("You have already left a review for this job.");

            var job = await _context.Jobs.FindAsync(jobId);
            var reviewee = await _userManager.FindByIdAsync(revieweeId);

            if (job == null || reviewee == null) return NotFound();

            ViewBag.JobTitle = job.Title;
            ViewBag.RevieweeName = reviewee.FullName ?? reviewee.Email;

            var model = new Review { JobId = jobId, RevieweeId = revieweeId };
            return View(model);
        }

        // POST: /Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model)
        {
            var reviewer = await _userManager.GetUserAsync(User);
            model.ReviewerId = reviewer!.Id;
            model.CreatedAt = DateTime.UtcNow;

            // 1. Save the new Review
            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            // 2. RECALCULATE THE TARGET USER'S SCORE
            var targetUser = await _userManager.FindByIdAsync(model.RevieweeId);
            var allReviews = await _context.Reviews.Where(r => r.RevieweeId == targetUser!.Id).ToListAsync();

            targetUser!.TotalReviews = allReviews.Count;
            targetUser.AverageRating = Math.Round(allReviews.Average(r => r.Rating), 1); // Round to 1 decimal (e.g. 4.8)
            await _userManager.UpdateAsync(targetUser);

            // 3. PUSH REAL-TIME NOTIFICATION
            var notif = new Notification
            {
                UserId = targetUser.Id,
                Title = "New Review Received! ⭐",
                Message = $"Someone left you a {model.Rating}-star review for a recent job.",
                NotificationType = "System",

                
                ActionUrl = $"/Profile/User/{targetUser.Id}",

                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();

            await _notificationHub.Clients.User(targetUser.Id).SendAsync("ReceiveNotification", notif.Title, notif.Message, notif.ActionUrl);

            // 4. Send them back to their correct dashboard
            if (await _userManager.IsInRoleAsync(reviewer, "Employer")) return RedirectToAction("Index", "Dashboard");
            return RedirectToAction("WorkerDashboard", "Dashboard");
        }
    }
}