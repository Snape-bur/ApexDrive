using ApexDrive.Data;
using ApexDrive.Models;
using ApexDrive.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

//  Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//  Configure Identity + Roles
builder.Services
    .AddDefaultIdentity<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // easier for dev
    })
    .AddRoles<IdentityRole>() // enable roles: Admin, SuperAdmin, Customer
    .AddEntityFrameworkStores<ApplicationDbContext>();

//  MVC + Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization() // enable _localizer in views
    .AddDataAnnotationsLocalization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BranchScopeService>();

//  Configure Supported Cultures (English, Thai, Burmese)
var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("th-TH"),
    new CultureInfo("my-MM")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Use cookie to remember user's selected language
localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

var app = builder.Build();

//  Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//  Enable Localization Middleware
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

//  Map Routes (Areas + Default)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

//  Run Role + SuperAdmin Seeder at Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.Initialize(services);  // <--- Call your SeedData class here
}

// Run the App
await app.RunAsync();
