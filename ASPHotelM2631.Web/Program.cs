using System.Security.Claims;
using System.Text;
using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Application.Bookings;
using ASPHotelM2631.Infrastructure.Identity;
using ASPHotelM2631.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ASPHotelM2631.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key in configuration");
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer in configuration");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

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

            // JWT: phải khai báo SAU AddIdentity để ghi đè scheme mặc định (Identity mặc định dùng cookie)
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;   // giữ nguyên tên claim "sub", "role"...
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };

                    // Mỗi request: kiểm tra người dùng còn tồn tại, chưa bị khóa, và security stamp còn khớp
                    // (đăng xuất hoặc đổi vai trò sẽ đổi stamp => token cũ bị từ chối)
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userManager = context.HttpContext.RequestServices
                                .GetRequiredService<UserManager<ApplicationUser>>();
                            var id = context.Principal?.FindFirstValue("sub");
                            var stamp = context.Principal?.FindFirstValue(JwtTokenService.SecurityStampClaim);
                            var user = id is null ? null : await userManager.FindByIdAsync(id);

                            if (user is null || user.SecurityStamp != stamp || await userManager.IsLockedOutAsync(user))
                                context.Fail("Token không còn hiệu lực.");
                        }
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<ITokenService, JwtTokenService>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IAccountStore, IdentityAccountStore>();
            // Chưa cấu hình Smtp:Host thì dùng chế độ thử (in mã ra terminal)
            if (string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
                builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
            else
                builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

            builder.Services.AddScoped<IEmailVerification, EmailOtpService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<EfBookingStore>();
            builder.Services.AddScoped<IBookingStore>(services => services.GetRequiredService<EfBookingStore>());
            builder.Services.AddScoped<IGuestStore>(services => services.GetRequiredService<EfBookingStore>());
            builder.Services.AddScoped<BookingService>();
            builder.Services.AddScoped<GuestService>();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

            if (app.Environment.IsDevelopment())
            {
                await using var scope = app.Services.CreateAsyncScope();
                var services = scope.ServiceProvider;

                await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();
                await IdentitySeeder.SeedAsync(services, includeTestUsers: true);
            }

            app.Run();
        }
    }
}
