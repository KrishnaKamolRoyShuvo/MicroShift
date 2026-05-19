using Microsoft.AspNetCore.Identity;
using MicroShift.Models;

namespace MicroShift.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Ensure Role Exists
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                Console.WriteLine("✅ Admin Role Created.");
            }

            // Your new credentials
            string adminEmail = "admin112@microshift.com";
            string adminPassword = "Pass@1234"; 

            // 2. Look for the user
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Create brand new user
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "MicroShift Administrator",
                    EmailConfirmed = true 
                };

                var result = await userManager.CreateAsync(newAdmin, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    Console.WriteLine("✅ SUCCESS: Brand new Admin Account Created!");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"❌ SEEDER CREATION ERROR: {error.Description}");
                    }
                }
            }
            else
            {
                // USER ALREADY EXISTS - LET'S FORCE FIX THEM
                Console.WriteLine("ℹ️ Admin account found in DB. Forcing a password and status reset...");
                
                // Force Email Confirmation
                adminUser.EmailConfirmed = true;
                await userManager.UpdateAsync(adminUser);

                // Force Password Reset
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                var resetResult = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
                
                if (resetResult.Succeeded)
                {
                    Console.WriteLine("✅ SUCCESS: Password force-reset to match your code!");
                }
                else
                {
                    foreach (var error in resetResult.Errors)
                    {
                        Console.WriteLine($"❌ SEEDER RESET ERROR: {error.Description}");
                    }
                }

                // Ensure Role is attached
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("✅ Assigned Admin Role to existing user.");
                }
            }
        }
    }
}