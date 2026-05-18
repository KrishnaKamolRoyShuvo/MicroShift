using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models; // Critical for ApplicationUser

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MicroShiftContextConnection")
    ?? throw new InvalidOperationException("Connection string 'MicroShiftContextConnection' not found.");

// 1. Register our single Database Context
builder.Services.AddDbContext<MicroShiftDBContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configure Identity to strictly use ApplicationUser and our DB Context
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>() // Enables Roles (Worker/Employer)
    .AddEntityFrameworkStores<MicroShiftDBContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<MicroShift.Hubs.ChatHub>("/chatHub");
app.MapHub<MicroShift.Hubs.NotificationHub>("/notificationHub");

app.MapRazorPages();
app.Run();