// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using MicroShift.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MicroShift.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        // 1. WE ADDED THE USER MANAGER HERE
        private readonly UserManager<ApplicationUser> _userManager;

        // 2. WE INJECTED IT INTO THE CONSTRUCTOR
        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    // SECURITY LOCK 1: Are they already frozen from previous attempts?
                    if (user.IsAccountFrozen)
                    {
                        TempData["RecoveryEmail"] = user.Email;
                        return RedirectToAction("UnlockAccount", "Password");
                    }

                    // Attempt the actual login
                    var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // SUCCESS! Reset all penalty counters back to zero.
                        user.RecoveryFailedAttempts = 0;
                        user.RecoveryLockoutEnd = null;
                        await _userManager.UpdateAsync(user);

                        _logger.LogInformation("User logged in.");
                        return LocalRedirect(returnUrl ?? "~/");
                    }
                    else
                    {
                        // FAILED PASSWORD: Apply the 3-Strike Rule
                        user.RecoveryFailedAttempts++;

                        if (user.RecoveryFailedAttempts >= 3)
                        {
                            // Strike 3: Freeze the account!
                            user.IsAccountFrozen = true;
                            await _userManager.UpdateAsync(user);

                            // Send them straight to NID verification
                            TempData["RecoveryEmail"] = user.Email;
                            return RedirectToAction("UnlockAccount", "Password");
                        }

                        // Save the failed attempt and warn them
                        await _userManager.UpdateAsync(user);
                        ModelState.AddModelError(string.Empty, $"Invalid login attempt. You have {3 - user.RecoveryFailedAttempts} attempt(s) left before your account is frozen.");
                        return Page();
                    }
                }

                // If user doesn't exist, just show generic error so hackers can't farm emails
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}