using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Application.Rooms;
using ASPHotelM2631.Infrastructure.Identity;
using ASPHotelM2631.Infrastructure.Identity.Accounts;
using ASPHotelM2631.Infrastructure.Identity.EmailVerification;
using ASPHotelM2631.Infrastructure.Identity.EmailVerification.Senders;
using ASPHotelM2631.Infrastructure.Identity.Tokens;
using ASPHotelM2631.Infrastructure.Persistence;
using ASPHotelM2631.Infrastructure.Persistence.Rooms;
using ASPHotelM2631.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ASPHotelM2631.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Missing Jwt:Key in configuration.");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing Jwt:Issuer in configuration.");

        // Database
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Identity and authentication
        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Register JWT authentication after AddIdentity so JWT is the default scheme.
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                // Check that the account still exists, is not locked, and has the same security stamp.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();
                        var userId = context.Principal?.FindFirstValue("sub");
                        var tokenStamp = context.Principal?.FindFirstValue("securityStamp");
                        var user = userId is null
                            ? null
                            : await userManager.FindByIdAsync(userId);

                        if (user is null
                            || !user.IsActive
                            || user.SecurityStamp != tokenStamp
                            || await userManager.IsLockedOutAsync(user))
                        {
                            context.Fail("Token is no longer valid.");
                        }
                    }
                };
            });

        builder.Services.AddAuthorization();

        // Authentication services
        builder.Services.AddSingleton<ITokenService, JwtTokenService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAccountStore, IdentityAccountStore>();
        builder.Services.AddScoped<IEmailVerification, EmailOtpService>();
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

        if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
        {
            // Without SMTP settings, write verification emails to the application log.
            builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
        }
        else
        {
            builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }

        // Room search services
        builder.Services.AddScoped<IRoomQueries, RoomQueries>();
        builder.Services.AddScoped<IRoomSearchService, RoomSearchService>();

        // Limit public search requests by client IP (default: 60 requests per minute).
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("public-search", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = builder.Configuration.GetValue("RateLimit:PerMinute", 60),
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        // HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        // Apply migrations and seed development data.
        if (app.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;

            await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            await IdentitySeeder.SeedAsync(services, includeTestUsers: true);
            await RoomSeeder.SeedAsync(app.Services);
        }

        app.Run();
    }
}
