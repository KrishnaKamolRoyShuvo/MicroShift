using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models;

namespace MicroShift.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- PUBLIC & PRIVATE PROFILE VIEWER ---
        [HttpGet("Profile/User/{id}")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // SECURITY CHECK: Is the person viewing the page the owner of the profile?
            bool isOwnProfile = (currentUser != null && currentUser.Id == targetUser.Id);

            var roles = await _userManager.GetRolesAsync(targetUser);
            string primaryRole = roles.FirstOrDefault() ?? "Worker";

            // 1. Calculate Lifetime Financials
            decimal lifetimeVolume = 0;
            int jobsCompleted = 0;

            if (primaryRole == "Worker")
            {
                var workerApps = await _context.JobApplications
                    .Include(a => a.Job)
                    .Where(a => a.WorkerId == targetUser.Id && a.Status == "Completed")
                    .ToListAsync();

                lifetimeVolume = workerApps.Sum(a => a.Job!.PaymentAmount);
                jobsCompleted = workerApps.Count;
            }
            else if (primaryRole == "Employer")
            {
                var employerJobs = await _context.Jobs
                    .Where(j => j.EmployerId == targetUser.Id && j.Status == "Completed")
                    .ToListAsync();

                lifetimeVolume = employerJobs.Sum(j => j.PaymentAmount);
                jobsCompleted = employerJobs.Count;
            }

            // 2. The Gamification Tier Engine
            string userTier = "New";
            if (lifetimeVolume >= 5000 && lifetimeVolume < 20000)
            {
                userTier = "Professional";
            }
            else if (lifetimeVolume >= 20000)
            {
                if (targetUser.AverageRating >= 4.0 || targetUser.TotalReviews == 0)
                {
                    userTier = "Top Rated";
                }
                else
                {
                    userTier = "Professional";
                }
            }

            // 3. Fetch Written Reviews (Assuming your Review model has a Reviewer navigation property)
            // Uncomment this once your Review table is set up!
            
            var reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Where(r => r.RevieweeId == targetUser.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewBag.Reviews = reviews;
            

            // Pack the data for the View
            ViewBag.IsOwnProfile = isOwnProfile;
            ViewBag.PrimaryRole = primaryRole;
            ViewBag.UserTier = userTier;
            ViewBag.LifetimeVolume = lifetimeVolume;
            ViewBag.JobsCompleted = jobsCompleted;

            return View(targetUser);
        }
    }
}