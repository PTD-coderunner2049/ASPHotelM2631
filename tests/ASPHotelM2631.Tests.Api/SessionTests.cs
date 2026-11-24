using System.Net;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

/// <summary>
/// Các test token GIẢ/HẾT HẠN dễ "pass ảo" (nếu server từ chối MỌI token thì 401 vẫn đúng nhưng vì lý do sai).
/// Vì vậy mỗi test âm luôn có ĐỐI CHỨNG DƯƠNG: token thật phải được chấp nhận trước.
/// </summary>
public class SessionTests : IDisposable
{
    private readonly HotelApiFactory _f = new();
    private readonly HttpClient _c;
    public SessionTests() { _c = _f.Https(); }
    public void Dispose() { _c.Dispose(); _f.Dispose(); }

    private const string RealKey = "1234567890qwertyuiopasdfghjklzxcv";   // khoá trong appsettings.json

    private async Task AssertAccepted(string token) =>
        Assert.Equal(HttpStatusCode.OK, (await _c.SendAsync(ApiHelpers.Get("/api/sessions/current", token))).StatusCode);

    [TcFact("TC-API-SESS-001", "GET /api/sessions/current với token thật vừa đăng nhập -> 200 và đúng thông tin user", Priority = TcMeta.High,
        Pre = "Đã đăng ký + đăng nhập lấy token", Steps = "GET /api/sessions/current, header Authorization: Bearer <token>",
        Expected = "200; body có id, fullName='Tester', userName=email, role='KhachHang'")]
    public async Task Current_WithValidToken_Returns200()
    {
        var (email, token) = await _c.RegisterAndLogin();

        var r = await _c.SendAsync(ApiHelpers.Get("/api/sessions/current", token));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var json = await r.Json();
        Assert.Equal("Tester", json.GetProperty("fullName").GetString());
        Assert.Equal(email, json.GetProperty("userName").GetString());
        Assert.Equal("KhachHang", json.GetProperty("role").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("id").GetString()));
    }

    [TcTheory("TC-API-SESS-002", "GET /api/sessions/current không token / token rác -> 401", Priority = TcMeta.High,
        Steps = "GET với header Authorization = <giá trị>", Expected = "401")]
    [InlineData(null)]
    [InlineData("Bearer")]
    [InlineData("Bearer abc.def.ghi")]
    [InlineData("Bearer not-a-jwt")]
    public async Task Current_NoOrGarbageToken_Returns401(string? header)
    {
        await _c.RegisterAndLogin();                                      // đảm bảo hệ thống hoạt động bình thường

        var req = ApiHelpers.Get("/api/sessions/current");
        if (header is not null) req.Headers.TryAddWithoutValidation("Authorization", header);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(req)).StatusCode);
    }

    [TcFact("TC-API-SESS-003", "Token ký bằng KHOÁ KHÁC bị từ chối (401) dù có claim role=Admin", Priority = TcMeta.High,
        Steps = "Đối chứng: token thật -> 200 | Tự ký token bằng key sai | GET /api/sessions/current",
        Expected = "Token thật 200; token giả 401")]
    public async Task Current_TokenSignedWithWrongKey_Returns401()
    {
        var (_, realToken) = await _c.RegisterAndLogin();
        await AssertAccepted(realToken);                                  // đối chứng dương

        var forged = ApiHelpers.ForgeToken("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Get("/api/sessions/current", forged))).StatusCode);
    }

    [TcFact("TC-API-SESS-004", "Token ký đúng khoá nhưng ĐÃ HẾT HẠN bị từ chối (401)", Priority = TcMeta.High,
        Steps = "Đối chứng: token thật -> 200 | Tự ký token đúng key nhưng exp = 5 phút trước | GET",
        Expected = "Token thật 200; token hết hạn 401")]
    public async Task Current_ExpiredToken_Returns401()
    {
        var (_, realToken) = await _c.RegisterAndLogin();
        await AssertAccepted(realToken);                                  // đối chứng dương

        var expired = ApiHelpers.ForgeToken(RealKey, DateTime.UtcNow.AddMinutes(-20), DateTime.UtcNow.AddMinutes(-5));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Get("/api/sessions/current", expired))).StatusCode);
    }

    [TcFact("TC-API-SESS-005", "Đăng xuất: DELETE trả 204 và token cũ bị vô hiệu ngay", Priority = TcMeta.High,
        Steps = "Login | GET current (200) | DELETE /api/sessions/current | GET current lại bằng token cũ",
        Expected = "Trước: 200; DELETE: 204; Sau: 401")]
    public async Task Logout_RevokesToken()
    {
        var (_, token) = await _c.RegisterAndLogin();
        await AssertAccepted(token);                                      // đối chứng dương

        var del = await _c.SendAsync(ApiHelpers.Delete("/api/sessions/current", token));

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Get("/api/sessions/current", token))).StatusCode);
    }

    [TcFact("TC-API-SESS-006", "DELETE /api/sessions/current không token -> 401",
        Steps = "DELETE không header Authorization", Expected = "401")]
    public async Task Logout_WithoutToken_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Delete("/api/sessions/current"))).StatusCode);
    }

    [TcFact("TC-API-SESS-007", "Tài khoản đang bị khóa thì token cũ cũng không dùng được",
        Steps = "Login lấy token (đối chứng 200) | Sai mật khẩu 5 lần | GET current bằng token cũ", Expected = "Sau khi khóa: 401")]
    public async Task Current_AfterLockout_Returns401()
    {
        var (email, token) = await _c.RegisterAndLogin();
        await AssertAccepted(token);                                      // đối chứng dương

        for (var i = 0; i < 5; i++) await _c.Login(email, "SaiMatKhau1");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Get("/api/sessions/current", token))).StatusCode);
    }
}
