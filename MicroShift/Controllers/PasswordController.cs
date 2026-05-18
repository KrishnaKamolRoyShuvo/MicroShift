using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MicroShift.Models;
using System;
using System.Threading.Tasks;

namespace MicroShift.Controllers
{
    [AllowAnonymous]
    public class PasswordController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // --- 1. ENTER EMAIL ---
        [HttpGet]
        public IActionResult Forgot() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(string email)
        {
            if (string.IsNullOrEmpty(email)) return View();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction(nameof(Confirmation));

            TempData["RecoveryEmail"] = email;
            return RedirectToAction(nameof(VerifyLastPassword));
        }

        // --- 2. LAYER 1: LAST REMEMBERED PASSWORD ---
        [HttpGet]
        public IActionResult VerifyLastPassword()
        {
            if (TempData["RecoveryEmail"] == null) return RedirectToAction(nameof(Forgot));
            TempData.Keep("RecoveryEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLastPassword(string lastPassword)
        {
            var email = TempData["RecoveryEmail"]?.ToString();
            if (email == null) return RedirectToAction(nameof(Forgot));

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && !string.IsNullOrEmpty(lastPassword))
            {
                if (await _userManager.CheckPasswordAsync(user, lastPassword))
                {
                    return await TriggerSimulatedEmail(user);
                }
            }

            TempData["RecoveryEmail"] = email;
            return RedirectToAction(nameof(VerifyPhone));
        }

        // --- 3. LAYER 2: PHONE NUMBER VERIFICATION (GET) ---
        [HttpGet]
        public async Task<IActionResult> VerifyPhone()
        {
            var email = TempData["RecoveryEmail"]?.ToString();
            if (email == null) return RedirectToAction(nameof(Forgot));
            TempData.Keep("RecoveryEmail");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction(nameof(Forgot));

            // Setup Hint and Lockout Logic
            SetViewData(user);

            return View();
        }

        // --- 3. LAYER 2: PHONE NUMBER VERIFICATION (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(string phoneNumber)
        {
            var email = TempData["RecoveryEmail"]?.ToString();
            if (email == null) return RedirectToAction(nameof(Forgot));
            TempData.Keep("RecoveryEmail");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return View();

            if (user.IsAccountFrozen) return RedirectToAction(nameof(UnlockAccount));

            // Check if already locked out
            if (user.RecoveryLockoutEnd.HasValue && user.RecoveryLockoutEnd > DateTime.UtcNow)
            {
                SetViewData(user);
                ModelState.AddModelError("", $"Security Lockout: Try again in {ViewBag.LockoutMinutes} minutes.");
                return View();
            }

            if (user.PhoneNumber == phoneNumber)
            {
                user.RecoveryFailedAttempts = 0;
                user.RecoveryLockoutEnd = null;
                await _userManager.UpdateAsync(user);
                return await TriggerSimulatedEmail(user);
            }

            // Failure Logic
            user.RecoveryFailedAttempts++;
            ApplyBackoff(user);
            await _userManager.UpdateAsync(user);

            SetViewData(user);

            if (user.RecoveryFailedAttempts >= 3)
            {
                ModelState.AddModelError("", $"Incorrect. You are now locked out for {ViewBag.LockoutMinutes} minutes.");
            }
            else
            {
                ModelState.AddModelError("", $"Incorrect phone number. You have {3 - user.RecoveryFailedAttempts} attempt(s) left.");
            }

            return View();
        }

        // --- HELPER METHODS ---
        private void SetViewData(ApplicationUser user)
        {
            string p = user.PhoneNumber?.Trim() ?? "";
            ViewBag.PhoneHint = p.Length >= 2 ? p.Substring(p.Length - 2) : "XX";

            bool isLocked = user.RecoveryLockoutEnd.HasValue && user.RecoveryLockoutEnd > DateTime.UtcNow;
            ViewBag.IsLockedOut = isLocked;

            if (isLocked)
            {
                ViewBag.LockoutMinutes = Math.Ceiling((user.RecoveryLockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
            }
        }

        private void ApplyBackoff(ApplicationUser user)
        {
            if (user.RecoveryFailedAttempts == 3) user.RecoveryLockoutEnd = DateTime.UtcNow.AddMinutes(5);
            else if (user.RecoveryFailedAttempts == 4) user.RecoveryLockoutEnd = DateTime.UtcNow.AddMinutes(15);
            else if (user.RecoveryFailedAttempts == 5) user.RecoveryLockoutEnd = DateTime.UtcNow.AddMinutes(30);
            else if (user.RecoveryFailedAttempts == 6) user.RecoveryLockoutEnd = DateTime.UtcNow.AddHours(1);
            else if (user.RecoveryFailedAttempts == 7) user.RecoveryLockoutEnd = DateTime.UtcNow.AddHours(3);
            else if (user.RecoveryFailedAttempts >= 8)
            {
                user.RecoveryLockoutEnd = DateTime.UtcNow.AddHours(6);
                user.IsAccountFrozen = true;
            }
        }

        private async Task<IActionResult> TriggerSimulatedEmail(ApplicationUser user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("Reset", "Password", new { email = user.Email, token = token }, protocol: Request.Scheme);
            TempData["DevResetLink"] = callbackUrl;
            return RedirectToAction(nameof(Confirmation));
        }

        // --- OTHER VIEWS ---
        [HttpGet] public IActionResult Confirmation() => View();
        [HttpGet] public IActionResult ResetSuccess() => View();

        [HttpGet]
        public IActionResult Reset(string token, string email)
        {
            if (token == null || email == null) return BadRequest("Invalid token.");
            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(string email, string token, string newPassword, string confirmPassword)
        {
            ViewBag.Token = token;
            ViewBag.Email = email;
            if (newPassword != confirmPassword) { ModelState.AddModelError("", "Passwords do not match."); return View(); }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction(nameof(ResetSuccess));

            if (await _userManager.CheckPasswordAsync(user, newPassword))
            {
                ModelState.AddModelError("", "Security Alert: You cannot reuse your current password.");
                return View();
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded) return RedirectToAction(nameof(ResetSuccess));

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View();
        }

        [HttpGet]
        public IActionResult UnlockAccount()
        {
            if (TempData["RecoveryEmail"] == null) return RedirectToAction(nameof(Forgot));
            TempData.Keep("RecoveryEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string nationalId)
        {
            var email = TempData["RecoveryEmail"]?.ToString();
            if (email == null) return RedirectToAction(nameof(Forgot));
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && user.NationalIdNumber == nationalId)
            {
                user.IsAccountFrozen = false;
                user.RecoveryFailedAttempts = 0;
                user.RecoveryLockoutEnd = null;
                await _userManager.UpdateAsync(user);
                return await TriggerSimulatedEmail(user);
            }
            ModelState.AddModelError("", "NID mismatch. Account remains locked.");
            TempData.Keep("RecoveryEmail");
            return View();
        }
    }
}