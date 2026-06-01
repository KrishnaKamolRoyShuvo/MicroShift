using MicroShift.Data;
using MicroShift.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroShift.Controllers
{
    // Restrict this entire controller to only users with the "Admin" role
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // ADDED THIS

        // UPDATED CONSTRUCTOR
        public AdminController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            // 1. Fetch all jobs and users for global analytics
            var allJobs = await _context.Jobs.ToListAsync();
            var totalUsers = await _context.Users.CountAsync();

            // 2. FINANCIAL METRICS MATH
            // Total money that has ever passed through the platform (excluding cancelled)
            var totalCashflow = allJobs.Where(j => j.Status != "Cancelled").Sum(j => j.PaymentAmount);

            // Platform Revenue (Only completed jobs pay out commission)
            var completedJobs = allJobs.Where(j => j.Status == "Completed").ToList();
            var totalRevenue = completedJobs.Sum(j => j.PaymentAmount * (decimal)(j.FinalCommissionPercentage / 100.0));

            // Escrow / Ongoing Work (Money promised but not yet released)
            var ongoingMoney = allJobs.Where(j => j.Status == "Open" || j.Status == "InProgress").Sum(j => j.PaymentAmount);

            // Disputed Money (Locked funds awaiting admin resolution)
            var disputedMoney = allJobs.Where(j => j.Status == "Disputed").Sum(j => j.PaymentAmount);

            // 3. BUSINESS GROWTH ANALYTICS (This Month vs Last Month Revenue)
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var thisMonthRevenue = completedJobs
                .Where(j => j.CreatedAt.Month == currentMonth && j.CreatedAt.Year == currentYear)
                .Sum(j => j.PaymentAmount * (decimal)(j.FinalCommissionPercentage / 100.0));

            var lastMonthRevenue = completedJobs
                .Where(j => j.CreatedAt.Month == (currentMonth == 1 ? 12 : currentMonth - 1) &&
                            j.CreatedAt.Year == (currentMonth == 1 ? currentYear - 1 : currentYear))
                .Sum(j => j.PaymentAmount * (decimal)(j.FinalCommissionPercentage / 100.0));

            // Calculate percentage growth safely
            decimal growthPercentage = 0;
            if (lastMonthRevenue > 0)
            {
                growthPercentage = ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
            }
            else if (thisMonthRevenue > 0)
            {
                growthPercentage = 100; // 100% growth if last month was 0
            }

            // 4. CHART DATA PREPARATION (Last 6 Months Revenue)
            var monthlyRevenueData = new List<decimal>();
            var monthLabels = new List<string>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = DateTime.UtcNow.AddMonths(-i);
                monthLabels.Add(targetDate.ToString("MMM")); // e.g. "Jan", "Feb"

                var rev = completedJobs
                    .Where(j => j.CreatedAt.Month == targetDate.Month && j.CreatedAt.Year == targetDate.Year)
                    .Sum(j => j.PaymentAmount * (decimal)(j.FinalCommissionPercentage / 100.0));

                monthlyRevenueData.Add(rev);
            }

            // 5. URGENCY TRACKER & RECENT TRANSACTIONS
            var emergencyJobs = allJobs.Where(j => j.IsEmergency && j.Status == "Open").OrderByDescending(j => j.CreatedAt).Take(5).ToList();
            var recentTransactions = allJobs.Where(j => j.Status == "Completed").OrderByDescending(j => j.CreatedAt).Take(5).ToList();

            // PACK EVERYTHING INTO VIEW-BAG
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalCashflow = totalCashflow;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.OngoingMoney = ongoingMoney;
            ViewBag.DisputedMoney = disputedMoney;
            ViewBag.GrowthPercentage = Math.Round(growthPercentage, 1);
            ViewBag.ThisMonthRevenue = thisMonthRevenue;

            // Data for Charts
            ViewBag.MonthLabels = monthLabels;
            ViewBag.MonthlyRevenueData = monthlyRevenueData;
            ViewBag.StatusDistribution = new[] {
                allJobs.Count(j => j.Status == "Open"),
                allJobs.Count(j => j.Status == "InProgress"),
                allJobs.Count(j => j.Status == "Completed"),
                allJobs.Count(j => j.Status == "Disputed")
            };

            ViewBag.EmergencyJobs = emergencyJobs;
            ViewBag.RecentTransactions = recentTransactions;

            return View();
        }

        // --- 1. THE DISPUTE QUEUE ---
        public async Task<IActionResult> Disputes()
        {
            // Fetch all jobs currently marked as Disputed
            var disputes = await _context.Jobs
                .Include(j => j.Employer)
                .Where(j => j.Status == "Disputed")
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(disputes);
        }

        // --- 2. THE COURTROOM (View Case Details) ---
        public async Task<IActionResult> DisputeDetails(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.JobApplications).ThenInclude(a => a.Worker) // Pull the assigned worker
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null) return NotFound();

            return View(job);
        }

        // --- 4. USER MODERATION DIRECTORY ---
        public async Task<IActionResult> Users(string searchQuery, string roleFilter, string disputeFilter)
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserModerationViewModel>();

            // Pre-load jobs to calculate metrics safely without crashing the DB
            var allJobs = await _context.Jobs.Include(j => j.JobApplications).ToListAsync();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                string primaryRole = roles.FirstOrDefault() ?? "None";

                // Filter by Role early to save processing
                if (!string.IsNullOrEmpty(roleFilter) && primaryRole != roleFilter) continue;

                // Search Filter (Name or Email)
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    bool matchName = u.FullName != null && u.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchEmail = u.Email != null && u.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    if (!matchName && !matchEmail) continue;
                }

                // Calculate Metrics based on Role
                decimal earnings = 0;
                decimal ongoing = 0;
                int completed = 0;
                int disputes = 0;
                int apps = 0;

                if (primaryRole == "Worker")
                {
                    var workerApps = allJobs.SelectMany(j => j.JobApplications).Where(a => a.WorkerId == u.Id).ToList();
                    apps = workerApps.Count;

                    var acceptedJobIds = workerApps.Where(a => a.Status == "Accepted").Select(a => a.JobId).ToList();
                    var workerJobs = allJobs.Where(j => acceptedJobIds.Contains(j.Id)).ToList();

                    earnings = workerJobs.Where(j => j.Status == "Completed").Sum(j => j.PaymentAmount);
                    ongoing = workerJobs.Where(j => j.Status == "InProgress").Sum(j => j.PaymentAmount);
                    completed = workerJobs.Count(j => j.Status == "Completed");
                    disputes = workerJobs.Count(j => j.Status == "Disputed" || j.FaultAssignedTo == "Worker");
                }
                else if (primaryRole == "Employer")
                {
                    var employerJobs = allJobs.Where(j => j.EmployerId == u.Id).ToList();
                    ongoing = employerJobs.Where(j => j.Status == "Open" || j.Status == "InProgress").Sum(j => j.PaymentAmount);
                    completed = employerJobs.Count(j => j.Status == "Completed");
                    disputes = employerJobs.Count(j => j.Status == "Disputed" || j.FaultAssignedTo == "Employer");
                }

                // Filter by Dispute History
                if (disputeFilter == "HasDisputes" && disputes == 0) continue;

                userList.Add(new UserModerationViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "No Name",
                    Email = u.Email ?? "",
                    Role = primaryRole,
                    TotalEarnings = earnings,
                    OngoingWorkValue = ongoing,
                    TotalCompleted = completed,
                    ApplicationsCount = apps,
                    DisputeCount = disputes,
                    IsSuspended = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            ViewBag.SearchQuery = searchQuery;
            return View(userList);
        }

        // --- 5. FORCE RESET CREDENTIALS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, newPassword);
            }
            return RedirectToAction(nameof(Users));
        }

        // --- 6. SUSPEND / UNSUSPEND USER ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSuspend(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null); // Unban
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100)); // Ban
                }
            }
            return RedirectToAction(nameof(Users));
        }

        // --- 3. PROCESS THE RULING ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(int jobId, string resolutionType, string faultAssignedTo, string adminNote, decimal? newPaymentAmount)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();

            // Save the Admin's internal notes and fault assignment
            job.AdminDisputeNote = adminNote;
            job.FaultAssignedTo = faultAssignedTo;

            // Execute the specific ruling
            switch (resolutionType)
            {
                case "ForceComplete":
                    job.Status = "Completed"; // Worker gets paid full amount
                    break;
                case "CancelRefund":
                    job.Status = "Cancelled"; // Employer gets fully refunded
                    break;
                case "PartialSettlement":
                    job.Status = "Completed";
                    if (newPaymentAmount.HasValue)
                    {
                        job.PaymentAmount = newPaymentAmount.Value; // Override the payout amount
                    }
                    break;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Disputes)); // Kick them back to the queue
        }

        // --- 7. JOB MODERATION (God Mode Feed) ---
        public async Task<IActionResult> Jobs(string statusFilter = "Open")
        {
            // Fetch jobs with their Employer and Category details
            var jobs = _context.Jobs
                .Include(j => j.Employer)
                .Include(j => j.Category)
                .AsQueryable();

            // Default to showing Open jobs unless specified
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                jobs = jobs.Where(j => j.Status == statusFilter);
            }

            ViewBag.CurrentFilter = statusFilter;
            return View(await jobs.OrderByDescending(j => j.CreatedAt).ToListAsync());
        }

        // --- 8. FORCE TAKEDOWN A JOB ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeDownJob(int jobId, string reason)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job != null)
            {
                job.Status = "Cancelled";
                job.AdminDisputeNote = $"ADMIN TAKEDOWN: {reason}";

                // Note: If you have Stripe integrated, you would trigger the refund API here.

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Jobs));
        }

        // --- 8.5 RESTORE A TAKEN DOWN JOB ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreJob(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job != null && job.Status == "Cancelled")
            {
                job.Status = "Open"; // Bring it back to life
                job.AdminDisputeNote = null; // Clear the takedown reason

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Jobs));
        }

        // --- 9. PLATFORM SETTINGS (Category & Commission Control) ---
        public async Task<IActionResult> Settings()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        // --- 10. UPDATE COMMISSION PERCENTAGE ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(int id, double commissionPercentage)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Ensure commission stays between 0 and 100
                if (commissionPercentage >= 0 && commissionPercentage <= 100)
                {
                    category.CategoryCommissionPercentage = commissionPercentage;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Settings));
        }

        // --- 11. ADD NEW CATEGORY ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, double commissionPercentage)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var category = new Category
                {
                    Name = name,
                    CategoryCommissionPercentage = commissionPercentage
                };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Settings));
        }

        // --- 12. START IMPERSONATION (Using Secure Cookies) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartImpersonation(string userId)
        {
            // Find the user to determine their role
            var targetUser = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(targetUser);
            var primaryRole = roles.FirstOrDefault() ?? "Worker";

            // Drop secure cookies into the browser to activate God Mode
            Response.Cookies.Append("ImpersonatedUserId", userId, new CookieOptions { HttpOnly = true, Path = "/" });
            Response.Cookies.Append("IsImpersonating", "true", new CookieOptions { HttpOnly = true, Path = "/" });

            // NEW: Add a readable cookie so the Javascript knows which Dashboard URL to load!
            Response.Cookies.Append("ImpersonatedRole", primaryRole, new CookieOptions { HttpOnly = false, Path = "/" });

            return RedirectToAction("Dashboard", "Admin");
        }

        // --- 13. STOP IMPERSONATION ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StopImpersonation()
        {
            // Destroy all cookies to return to normal
            Response.Cookies.Delete("ImpersonatedUserId");
            Response.Cookies.Delete("IsImpersonating");
            Response.Cookies.Delete("ImpersonatedRole");

            return RedirectToAction("Dashboard", "Admin");
        }

        // --- 14. API ENDPOINT FOR IDENTITY SWITCHER MODAL ---
        [HttpGet]
        public async Task<IActionResult> GetUsersForImpersonation(string role)
        {
            try
            {
                var users = await _userManager.GetUsersInRoleAsync(role);
                var userList = users.Select(u => new
                {
                    id = u.Id,
                    fullName = u.FullName ?? "Unknown User",
                    email = u.Email
                }).OrderBy(u => u.fullName).ToList();

                return Json(userList);
            }
            catch (Exception ex)
            {
                // If it crashes, return the exact error message so we can see it in the console!
                return BadRequest(new { error = ex.Message });
            }
        }



        // --- 15. TRANSACTION LEDGER ---
        public async Task<IActionResult> Ledger()
        {
            // Fetch all transactions, including user details
            var allTransactions = await _context.Transactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(allTransactions);
        }



    }
}