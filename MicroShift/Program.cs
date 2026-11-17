using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models;

var builder = WebApplication.CreateBuilder(args);

// Resolve connection string from DATABASE_URL (Cloud/Neon) or appsettings (Local SQL Server)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL_UNPOOLED")
    ?? builder.Configuration.GetConnectionString("MicroShiftContextConnection")
    ?? throw new InvalidOperationException("Connection string 'MicroShiftContextConnection' or 'DATABASE_URL' not found.");

bool isPostgres = connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                  connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                  connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);

// 1. Register our Database Context (automatically selects PostgreSQL in cloud or SQL Server locally)
builder.Services.AddDbContext<MicroShiftDBContext>(options =>
{
    if (isPostgres)
    {
        var npgsqlConn = FormatNpgsqlConnectionString(connectionString);
        options.UseNpgsql(npgsqlConn);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// 2. Configure Identity to strictly use ApplicationUser and our DB Context
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MicroShiftDBContext>();

// Configure Forwarded Headers for reverse proxies (Render, Cloudflare)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddRazorPages();

// Configure Stripe if API key is provided
var stripeKey = builder.Configuration.GetSection("Stripe")["SecretKey"]
    ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
if (!string.IsNullOrWhiteSpace(stripeKey))
{
    Stripe.StripeConfiguration.ApiKey = stripeKey;
}

var app = builder.Build();

// Enable forwarded headers behind reverse proxies
app.UseForwardedHeaders();

// --- DATA SEEDER & SCHEMA VERIFICATION ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<MicroShiftDBContext>();
        logger.LogInformation("Ensuring database schema exists...");
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation("Database schema verified and ready.");

        await MicroShift.Data.DbSeeder.SeedAdminUserAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}
// -----------------------------------------

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

/// <summary>
/// Normalizes a postgres:// or postgresql:// URL to an Npgsql-compatible connection string with SSL.
/// </summary>
static string FormatNpgsqlConnectionString(string rawUrl)
{
    if (rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(rawUrl);
            var userInfo = uri.UserInfo.Split(':');
            var user = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            var sslRequired = uri.Query.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase)
                              || host.EndsWith("neon.tech", StringComparison.OrdinalIgnoreCase);

            var sslPart = sslRequired ? ";SSL Mode=Require;Trust Server Certificate=true" : "";

            return $"Host={host};Port={port};Database={database};Username={user};Password={password};Include Error Detail=true{sslPart}";
        }
        catch
        {
            return rawUrl;
        }
    }
    return rawUrl;
}
