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

    [TcFact("TC-API-SESS-006", "DELETE /api/sessions/current không token -> 401",
        Steps = "DELETE không header Authorization", Expected = "401")]
    public async Task Logout_WithoutToken_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Delete("/api/sessions/current"))).StatusCode);
    }
}
