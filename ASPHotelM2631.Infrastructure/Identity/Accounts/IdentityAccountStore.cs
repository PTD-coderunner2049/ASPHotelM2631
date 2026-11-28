using System.Security.Claims;
using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Domain.Constants;
using ASPHotelM2631.Domain.Entities;
using ASPHotelM2631.Infrastructure.Identity;
using ASPHotelM2631.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ASPHotelM2631.Infrastructure.Identity.Accounts;

public sealed class IdentityAccountStore(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    AppDbContext db,
    SignInManager<ApplicationUser> signIn,
    ITokenService tokens,
    IEmailVerification verification,
    IHttpContextAccessor http,
    ILogger<IdentityAccountStore> logger) : IAccountStore
{
    public async Task<AuthenticationStatus> CreateAsync(string fullName, string identifier, string password)
    {
        // 1. Bảo đảm vai trò KhachHang có sẵn (DB mới migrate chưa seed vẫn đăng ký được)
        await EnsureRoleAsync(RoleNames.Customer);

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

        // 2. Gán vai trò; lỗi thì xóa user vừa tạo để không để lại tài khoản mồ côi
        Guest? guestProfile = null;
        try
        {
            var roleResult = await users.AddToRoleAsync(user, RoleNames.Customer);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    "Không gán được vai trò: " + string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            guestProfile = new Guest
            {
                UserId = user.Id,
                FullName = fullName,
                Phone = isEmail ? "" : identifier,
                Email = isEmail ? identifier : ""
            };
            db.Guests.Add(guestProfile);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gán vai trò thất bại, xóa tài khoản {User} vừa tạo", identifier);
            if (guestProfile is not null)
                db.Entry(guestProfile).State = EntityState.Detached;
            await users.DeleteAsync(user);
            throw;
        }

        if (isEmail)
        {
            try
            {
                await verification.SendCodeAsync(identifier);
            }
            catch (Exception ex)
            {
                // Lỗi gửi mail không làm hỏng đăng ký; người dùng bấm "gửi lại mã" sau
                logger.LogError(ex, "Không gửi được mã OTP tới {Email}", identifier);
            }
        }

        return AuthenticationStatus.Succeeded;
    }

    public async Task<LoginResult> SignInAsync(string identifier, string password)
    {
        var user = await users.FindByNameAsync(identifier);
        if (user is null || !user.IsActive) return AuthenticationStatus.InvalidCredentials;

        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut) return AuthenticationStatus.LockedOut;
        if (!result.Succeeded) return AuthenticationStatus.InvalidCredentials;

        if (user.Email is not null && !user.EmailConfirmed)
            return AuthenticationStatus.EmailNotConfirmed;

        var userRoles = await users.GetRolesAsync(user);
        var (token, expires) = tokens.CreateToken(
            user.Id, user.UserName!, user.FullName, userRoles, user.SecurityStamp!);

        return new LoginResult(AuthenticationStatus.Succeeded, user.Id, user.FullName,
            userRoles.FirstOrDefault(), token, expires);
    }

    public async Task SignOutAsync()
    {
        var id = http.HttpContext?.User.FindFirstValue("sub");
        if (id is null) return;

        var user = await users.FindByIdAsync(id);
        if (user is not null) await users.UpdateSecurityStampAsync(user);
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (await roles.RoleExistsAsync(role)) return;

        try
        {
            await roles.CreateAsync(new IdentityRole(role));
        }
        catch (DbUpdateException)
        {
            // Hai request đăng ký đầu tiên cùng lúc: một bên tạo trước, bên kia lỗi trùng; bỏ qua
        }

        if (!await roles.RoleExistsAsync(role))
            throw new InvalidOperationException($"Không tạo được vai trò {role}.");
    }
}
