using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ASPHotelM2631.Infrastructure.Identity;

public sealed class IdentityAccountStore(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    IHttpContextAccessor http) : IAccountStore
{
    public async Task<AuthenticationStatus> CreateAsync(string fullName, string identifier, string password)
    {
        var isEmail = identifier.Contains('@');
        var user = new ApplicationUser
        {
            UserName = identifier,                       // đăng nhập bằng UserName
            FullName = fullName,
            Email = isEmail ? identifier : null,
            PhoneNumber = isEmail ? null : identifier
        };

        var result = await users.CreateAsync(user, password);   // Identity tự băm mật khẩu
        if (!result.Succeeded)
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName))
                ? AuthenticationStatus.DuplicateAccount
                : AuthenticationStatus.InvalidInput;

        await users.AddToRoleAsync(user, RoleNames.Customer);   // đăng ký công khai luôn là khách hàng
        return AuthenticationStatus.Succeeded;
    }

    public async Task<LoginResult> SignInAsync(string identifier, string password)
    {
        var user = await users.FindByNameAsync(identifier);
        if (user is null) return AuthenticationStatus.InvalidCredentials;

        // Kiểm tra mật khẩu + đếm số lần sai + khóa tài khoản + gửi cookie, tất cả trong một lệnh
        var result = await signIn.PasswordSignInAsync(user, password,
            isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut) return AuthenticationStatus.LockedOut;
        if (!result.Succeeded) return AuthenticationStatus.InvalidCredentials;

        var roles = await users.GetRolesAsync(user);
        return new LoginResult(AuthenticationStatus.Succeeded, user.Id, user.FullName, roles.FirstOrDefault());
    }

    public async Task SignOutAsync()
    {
        // Đổi security stamp để mọi cookie cũ của người này mất hiệu lực ngay phía server
        // (kể cả khi ai đó đã chép trộm cookie). Đổi lại: đăng xuất luôn cả các thiết bị khác.
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(principal);
            if (user is not null) await users.UpdateSecurityStampAsync(user);
        }
        await signIn.SignOutAsync();   // xóa cookie ở trình duyệt
    }
}