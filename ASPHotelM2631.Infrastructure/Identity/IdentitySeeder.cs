using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ASPHotelM2631.Infrastructure.Identity;

public static class IdentitySeeder
{
    /// <param name="includeTestUsers">
    /// true chỉ khi chạy Development: tạo tài khoản nhân viên thử.
    /// Môi trường thật thì nhân viên do Admin/Quản lý cấp, không dùng tài khoản mẫu.
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

        await EnsureUserAsync(users, "letan@hotel.test", "Lễ Tân Thử", "Staff@123", RoleNames.Receptionist);
        await EnsureUserAsync(users, "admin@hotel.test", "Quản Trị Thử", "Staff@123", RoleNames.Admin);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> users, string email, string fullName, string password, string role)
    {
        if (await users.FindByNameAsync(email) is not null) return;
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
        if ((await users.CreateAsync(user, password)).Succeeded)
            await users.AddToRoleAsync(user, role);
    }
}