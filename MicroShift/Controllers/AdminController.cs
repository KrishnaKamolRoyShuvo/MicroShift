using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models;

namespace MicroShift.Controllers
{
    // Restrict this entire controller to only users with the "Admin" role
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MicroShiftDBContext _context;

        public AdminController(MicroShiftDBContext context)
        {
            _context = context;
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
    }
}