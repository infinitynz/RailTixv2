using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RailTix.Data;
using RailTix.Models.Domain;
using RailTix.Models.Options;
using RailTix.Services.Cms;
using RailTix.Services.Email;
using RailTix.Services.Recaptcha;
using RailTix.Services.Location;
using RailTix.Services.Geo;
using RailTix.Middleware;
using Serilog;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
	// Enable runtime compilation of Razor views so .cshtml edits appear on refresh
	mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Serilog file logging - rolling daily logs under Logs/
var logsPath = Path.Combine(builder.Environment.ContentRootPath, "Logs", "log-.txt");
Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.WriteTo.File(logsPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, shared: true,
		outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
	.CreateLogger();
builder.Host.UseSerilog();

var identityBuilder = builder.Services
	.AddDefaultIdentity<ApplicationUser>(options =>
	{
		options.SignIn.RequireConfirmedAccount = true;
		options.User.RequireUniqueEmail = true;

		// Strong password policy
		options.Password.RequiredLength = 10;
		options.Password.RequireDigit = true;
		options.Password.RequireUppercase = true;
		options.Password.RequireLowercase = true;
		options.Password.RequireNonAlphanumeric = true;
		options.Password.RequiredUniqueChars = 5;

		options.Lockout.MaxFailedAccessAttempts = 5;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

// Custom password validator (blocks common passwords and personal info)
identityBuilder.AddPasswordValidator<RailTix.Services.Security.StrongPasswordValidator>();

// Ensure strong hashing parameters (PBKDF2 iteration count)
builder.Services.Configure<PasswordHasherOptions>(o =>
{
	o.IterationCount = 120000;
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<GoogleRecaptchaOptions>(builder.Configuration.GetSection("Recaptcha"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddHttpClient<IGoogleRecaptchaService, GoogleRecaptchaService>();
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddHttpClient<IGeoIpService, GeoIpService>();
builder.Services.AddScoped<ILocationProvider, LocationProvider>();
builder.Services.AddScoped<ICmsUrlService, CmsUrlService>();
builder.Services.AddScoped<ICmsReservedRouteService, CmsReservedRouteService>();
builder.Services.AddScoped<CmsPageRenderer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<CmsUrlNormalizationMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

// Seed identities (roles) at startup
using (var scope = app.Services.CreateScope())
{
    IdentityDataSeeder.SeedRolesAsync(scope).GetAwaiter().GetResult();
}

app.Run();
