using System.Security.Claims;
using System.Text.Encodings.Web;
using HotelManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelManagement.Infrastructure.Authentication;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    HotelDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Session";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SqlAccountStore.CookieName, out var token) ||
            string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = SessionTokens.Hash(token);
        var now = DateTime.Now;
        var customer = await db.KhachHang
            .AsNoTracking()
            .Where(user => user.TrangThai == "HoatDong" &&
                db.PhienDangNhap.Any(session =>
                    session.MaKH == user.MaKH &&
                    session.TokenHash == hash &&
                    !session.ThuHoi &&
                    session.HetHan > now))
            .FirstOrDefaultAsync();

        if (customer is null)
        {
            return AuthenticateResult.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
        }

        var expires = now + SqlAccountStore.SessionFor;
        var extendedSessions = await db.PhienDangNhap
            .Where(session =>
                session.TokenHash == hash &&
                !session.ThuHoi &&
                session.HetHan > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(session => session.HetHan, expires));
        if (extendedSessions == 0)
        {
            return AuthenticateResult.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
        }

        Response.Cookies.Append(SqlAccountStore.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            IsEssential = true,
            Path = "/",
            Expires = new DateTimeOffset(expires)
        });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.MaKH.ToString()),
            new("display_name", customer.HoTen)
        };
        if (customer.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, customer.Email));
        }

        if (customer.SoDienThoai is not null)
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, customer.SoDienThoai));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
