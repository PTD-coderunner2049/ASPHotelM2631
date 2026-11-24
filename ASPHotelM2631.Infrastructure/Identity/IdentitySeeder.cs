using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ASPHotelM2631.Infrastructure.Identity;

public static class IdentitySeeder
{
    private const string TestPassword = "Staff@123";

    /// <param name="includeTestUsers">
    /// true chỉ khi chạy Development: tạo các tài khoản thử (đã xác thực email sẵn).
    /// Môi trường thật thì chỉ tạo vai trò, nhân viên do Admin/Quản lý cấp.
    /// </param>
    public static async Task SeedAsync(IServiceProvider services, bool includeTestUsers = false)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Vai trò luôn phải có (đăng ký khách hàng cần vai trò KhachHang)
        foreach (var role in RoleNames.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        if (!includeTestUsers) return;

        await EnsureUserAsync(users, "khach@hotel.test",      "Khách Hàng Thử",   RoleNames.Customer);
        await EnsureUserAsync(users, "letan@hotel.test",      "Lễ Tân Thử",       RoleNames.Receptionist);
        await EnsureUserAsync(users, "buongphong@hotel.test", "Buồng Phòng Thử",  RoleNames.Housekeeping);
        await EnsureUserAsync(users, "ketoan@hotel.test",     "Kế Toán Thử",      RoleNames.Accountant);
        await EnsureUserAsync(users, "manager@hotel.test",    "Quản Lý Thử",      RoleNames.Manager);
        await EnsureUserAsync(users, "admin@hotel.test",      "Quản Trị Thử",     RoleNames.Admin);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> users, string email, string fullName, string role)
    {
        var user = await users.FindByNameAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true       // tài khoản test dùng được ngay, không cần OTP
            };
            if (!(await users.CreateAsync(user, TestPassword)).Succeeded) return;
        }
        else if (!user.EmailConfirmed)
        {
            // Tài khoản đã có (ví dụ đã đăng ký qua API): đánh dấu đã xác thực
            user.EmailConfirmed = true;
            await users.UpdateAsync(user);
        }

        if (!await users.IsInRoleAsync(user, role))
            await users.AddToRoleAsync(user, role);
    }
}
