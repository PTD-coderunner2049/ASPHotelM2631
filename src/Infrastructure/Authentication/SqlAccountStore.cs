using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelManagement.Application.Authentication;
using HotelManagement.Domain.Entities;
using HotelManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Authentication;

public static class SessionTokens
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed class SqlAccountStore(HotelDbContext db, IHttpContextAccessor http) : IUserAccountStore
{
    public const string CookieName = "HotelManagement.Auth";
    private const int MaxFailed = 5;
    private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan SessionFor = TimeSpan.FromMinutes(30);

    public async Task<AuthenticationStatus> CreateAsync(UserAccount account, string password)
    {
        var customer = new KhachHang
        {
            HoTen = account.FullName,
            Email = account.Email,
            SoDienThoai = account.PhoneNumber,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            TrangThai = "HoatDong",
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };
        db.KhachHang.Add(customer);

        try
        {
            await db.SaveChangesAsync();
            return AuthenticationStatus.Succeeded;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return AuthenticationStatus.DuplicateAccount;
        }
    }

    public async Task<AuthenticationStatus> SignInAsync(string identifier, string password)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var customer = await db.KhachHang
            .FromSqlInterpolated($"""
                SELECT *
                FROM dbo.KhachHang WITH (UPDLOCK, ROWLOCK)
                WHERE Email = {identifier} OR SoDienThoai = {identifier}
                """)
            .AsTracking()
            .FirstOrDefaultAsync();
        var now = DateTime.Now;

        if (customer is null)
        {
            await LogAsync(null, identifier, "KhongTonTai", now);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return AuthenticationStatus.InvalidCredentials;
        }

        if (customer.TrangThai == "TamKhoa")
        {
            if (customer.KhoaDen > now)
            {
                await LogAsync(customer.MaKH, identifier, "TaiKhoanBiKhoa", now);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return AuthenticationStatus.LockedOut;
            }

            customer.TrangThai = "HoatDong";
            customer.SoLanSaiLienTiep = 0;
            customer.KhoaDen = null;
        }

        if (customer.TrangThai != "HoatDong")
        {
            await LogAsync(customer.MaKH, identifier, "TaiKhoanBiKhoa", now);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return AuthenticationStatus.InvalidCredentials;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, customer.MatKhauHash))
        {
            customer.SoLanSaiLienTiep = (byte)Math.Min(customer.SoLanSaiLienTiep + 1, MaxFailed);
            customer.NgayCapNhat = now;
            var locked = customer.SoLanSaiLienTiep >= MaxFailed;
            if (locked)
            {
                customer.TrangThai = "TamKhoa";
                customer.KhoaDen = now + LockFor;
            }

            Log(customer.MaKH, identifier, "SaiMatKhau", now);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return locked ? AuthenticationStatus.LockedOut : AuthenticationStatus.InvalidCredentials;
        }

        customer.SoLanSaiLienTiep = 0;
        customer.KhoaDen = null;
        customer.NgayCapNhat = now;

        var context = http.HttpContext
            ?? throw new InvalidOperationException("No active HTTP request is available.");
        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));
        var expires = now + SessionFor;
        db.PhienDangNhap.Add(new PhienDangNhap
        {
            MaKH = customer.MaKH,
            TokenHash = SessionTokens.Hash(token),
            DiaChiIP = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 255),
            NgayTao = now,
            HetHan = expires,
            ThuHoi = false
        });
        Log(customer.MaKH, identifier, "ThanhCong", now);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            IsEssential = true,
            Path = "/",
            Expires = new DateTimeOffset(expires)
        });
        return AuthenticationStatus.Succeeded;
    }

   public async Task SignOutAsync()
{
    var context = http.HttpContext
        ?? throw new InvalidOperationException("No active HTTP request is available.");

    // 1. Thu hồi phiên hiện tại trong DB
    if (context.Request.Cookies.TryGetValue(CookieName, out var token)
        && !string.IsNullOrEmpty(token))
    {
        var hash = SessionTokens.Hash(token);   // cùng hàm băm lúc đăng nhập
        await db.PhienDangNhap
            .Where(p => p.TokenHash == hash && !p.ThuHoi)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.ThuHoi, true)
                .SetProperty(p => p.NgayThuHoi, DateTime.Now));
    }

    // 2. Xóa cookie khỏi trình duyệt (Path/SameSite phải khớp lúc tạo)
    context.Response.Cookies.Delete(CookieName, new CookieOptions
    {
        Path = "/",
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps
    });
}

    private async Task LogAsync(int? customerId, string identifier, string result, DateTime now)
    {
        Log(customerId, identifier, result, now);
        await db.SaveChangesAsync();
    }

    private void Log(int? customerId, string identifier, string result, DateTime now)
    {
        var context = http.HttpContext
            ?? throw new InvalidOperationException("No active HTTP request is available.");
        db.LichSuDangNhap.Add(new LichSuDangNhap
        {
            MaKH = customerId,
            DinhDanhNhap = Truncate(identifier, 150)!,
            KetQua = result,
            DiaChiIP = Truncate(context.Connection.RemoteIpAddress?.ToString(), 45),
            UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 255),
            ThoiGian = now
        });
    }

    private static string? Truncate(string? value, int max) =>
        value is { Length: > 0 } && value.Length > max ? value[..max] : value;
}
