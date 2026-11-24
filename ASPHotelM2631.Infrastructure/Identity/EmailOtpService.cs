using System.Security.Cryptography;
using System.Text;
using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ASPHotelM2631.Infrastructure.Identity;

public sealed class EmailOtpService(
    UserManager<ApplicationUser> users,
    AppDbContext db,
    IEmailSender mail,
    IConfiguration config) : IEmailVerification
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxFailedAttempts = 5;

    public async Task<AuthenticationStatus> SendCodeAsync(string identifier)
    {
     var user = await users.FindByNameAsync(identifier);
        if (user is null || user.Email is null || user.EmailConfirmed)
            return AuthenticationStatus.Succeeded;   
            // không tiết lộ tài khoản có tồn tại hay không bằng user is null để người ngoài không thể check đc xem đó có hợp lệ hay không

        var now = DateTime.UtcNow; // dùng UTC để tránh timezone khác nhau gây lỗi
        var last = await db.EmailOtps // lấy mã OTP gần nhất của user
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(); 
        if (last is not null && now - last.CreatedAt < ResendCooldown)
            return AuthenticationStatus.TooManyRequests; // chặn spam mail

        // Mã cũ chưa dùng bị vô hiệu
        await db.EmailOtps
            .Where(o => o.UserId == user.Id && o.UsedAt == null) 
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.UsedAt, now)); 

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");   // 000000 đến 999999
        db.EmailOtps.Add(new EmailOtp
        {
            UserId = user.Id,
            CodeHash = Hash(user.Id, code),
            CreatedAt = now,
            ExpiresAt = now + Lifetime
        });
        await db.SaveChangesAsync();

        await mail.SendAsync(user.Email, "Mã xác thực email",
            $"<p>Mã xác thực email của bạn là:</p><h2 style=\"letter-spacing:4px\">{code}</h2>" +
            "<p>Mã có hiệu lực 5 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>");
        return AuthenticationStatus.Succeeded;
    }

    public async Task<AuthenticationStatus> ConfirmAsync(string identifier, string code)
    {
        var user = await users.FindByNameAsync(identifier);
        if (user is null || user.EmailConfirmed) return AuthenticationStatus.InvalidCode;

        var now = DateTime.UtcNow;
        var otp = await db.EmailOtps
            .Where(o => o.UserId == user.Id && o.UsedAt == null && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        if (otp is null || otp.FailedAttempts >= MaxFailedAttempts)
            return AuthenticationStatus.InvalidCode;

        var expected = Encoding.UTF8.GetBytes(otp.CodeHash);
        var actual = Encoding.UTF8.GetBytes(Hash(user.Id, code));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            // Tăng bộ đếm trong một câu UPDATE để nhiều request cùng lúc không đoán lách được
            await db.EmailOtps.Where(o => o.Id == otp.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.FailedAttempts, o => o.FailedAttempts + 1));
            return AuthenticationStatus.InvalidCode;
        }

        otp.UsedAt = now;
        user.EmailConfirmed = true;
        await users.UpdateAsync(user);   // lưu cả hai thay đổi (cùng một DbContext)
        return AuthenticationStatus.Succeeded;
    }

     private string Hash(string userId, string code)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]
            ?? throw new InvalidOperationException("Thiếu Jwt:Key."));
        return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{userId}:{code}")));
    }
}
