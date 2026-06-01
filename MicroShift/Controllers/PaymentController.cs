using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using MicroShift.Data;
using MicroShift.Models;

namespace MicroShift.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. REDIRECT TO STRIPE CHECKOUT ---
        [HttpPost]
        [Authorize(Roles = "Employer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(int applicationId)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job == null) return NotFound();

            // The URL the user returns to after successfully paying
            var domain = $"{Request.Scheme}://{Request.Host}";
            var successUrl = domain + $"/Payment/PaymentSuccess?applicationId={application.Id}";
            var cancelUrl = domain + "/Dashboard/Index";

            // Setup the Stripe Checkout Options
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(application.Job.PaymentAmount * 100), // Stripe expects amounts in cents/paisa
                            Currency = "bdt", // BDT for Taka, or use "usd" for Dollars
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"MicroShift Payment: {application.Job.Title}",
                                Description = "Escrow release and final payment to worker."
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };

            var service = new SessionService();
            Session session = service.Create(options);

            // Redirect the user to Stripe's secure hosted checkout page
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // --- 2. PAYMENT SUCCESS CALLBACK ---
        // Stripe sends them back here after their card is approved
        public async Task<IActionResult> PaymentSuccess(int applicationId)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.Worker) // Include the worker to update their wallet
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application != null && application.Job != null && application.Status == "ReviewPending")
            {
                // 1. Mark Job as Completed
                application.Status = "Completed";
                application.Job.Status = "Completed";

                // 2. Do the Math
                decimal totalAmount = application.Job.PaymentAmount;
                decimal commissionRate = (decimal)(application.Job.FinalCommissionPercentage / 100.0);
                decimal platformCut = totalAmount * commissionRate;
                decimal workerEarnings = totalAmount - platformCut;

                // 3. Update Worker's Wallet
                application.Worker.WalletBalance += workerEarnings;

                // 4. Record the Worker's Earnings in the Ledger
                var workerTransaction = new Transaction
                {
                    UserId = application.WorkerId,
                    Amount = workerEarnings,
                    Type = "Credit",
                    Description = $"Earnings for '{application.Job.Title}'",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Transactions.Add(workerTransaction);

                // 5. Record the Platform's Commission in the Ledger (Assigned to the Employer's record for tracking)
                var commissionTransaction = new Transaction
                {
                    UserId = application.Job.EmployerId,
                    Amount = platformCut,
                    Type = "PlatformFee",
                    Description = $"Commission fee for '{application.Job.Title}'",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Transactions.Add(commissionTransaction);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment successful! The funds have been released to the worker.";
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}