using System.Security.Claims;
using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ASPHotelM2631.Infrastructure.Identity;

public sealed class IdentityAccountStore(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    ITokenService tokens,
    IHttpContextAccessor http) : IAccountStore
{
    public async Task<AuthenticationStatus> CreateAsync(string fullName, string identifier, string password)
    {
        var isEmail = identifier.Contains('@');
        var user = new ApplicationUser
        {
            UserName = identifier,
            FullName = fullName,
            Email = isEmail ? identifier : null,
            PhoneNumber = isEmail ? null : identifier
        };

        var result = await users.CreateAsync(user, password);
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

        // Chỉ kiểm tra mật khẩu và đếm số lần sai/khóa, KHÔNG phát cookie (khác PasswordSignInAsync)
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut) return AuthenticationStatus.LockedOut;
        if (!result.Succeeded) return AuthenticationStatus.InvalidCredentials;

        var roles = await users.GetRolesAsync(user);
        var (token, expires) = tokens.CreateToken(
            user.Id, user.UserName!, user.FullName, roles, user.SecurityStamp!);

        return new LoginResult(AuthenticationStatus.Succeeded, user.Id, user.FullName,
            roles.FirstOrDefault(), token, expires);
    }

    public async Task SignOutAsync()
    {
        // JWT là "không trạng thái": đổi security stamp để mọi token cũ của người này mất hiệu lực.
        // Đổi lại: đăng xuất cũng đăng xuất luôn các thiết bị khác.
        var id = http.HttpContext?.User.FindFirstValue("sub");
        if (id is null) return;

        var user = await users.FindByIdAsync(id);
        if (user is not null) await users.UpdateSecurityStampAsync(user);
    }
}