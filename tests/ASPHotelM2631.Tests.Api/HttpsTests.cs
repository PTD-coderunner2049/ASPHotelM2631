using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

/// <summary>
/// Kiểm tra hành vi HTTPS ở tầng ứng dụng (redirect, HSTS) ngay trong tiến trình test.
/// Phần TLS thật (chứng chỉ, handshake) nằm ở LiveHttpsTests.
/// </summary>
public class HttpsTests
{
    [TcFact("TC-API-TLS-001", "Gọi API bằng HTTP bị chuyển hướng sang HTTPS (307), giữ nguyên đường dẫn", Priority = TcMeta.High,
        Steps = "GET http://localhost/api/customer (không tự theo redirect)",
        Expected = "307; Location bắt đầu bằng https:// và kết thúc bằng /api/customer")]
    public async Task Http_RedirectsToHttps()
    {
        using var f = new HotelApiFactory();
        using var c = f.Http();

        var r = await c.GetAsync("/api/customer");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, r.StatusCode);
        var location = r.Headers.Location!.ToString();
        Assert.StartsWith("https://", location);
        Assert.EndsWith("/api/customer", location);
    }

    [TcFact("TC-API-TLS-002", "Đăng nhập qua HTTP KHÔNG được xử lý: bị redirect và không trả token", Priority = TcMeta.High,
        Pre = "Tài khoản đã tồn tại (đối chứng: login qua HTTPS thành công)",
        Steps = "Đăng ký + login qua HTTPS (201) | POST /api/sessions qua HTTP",
        Expected = "Qua HTTPS: 201. Qua HTTP: 307, body không chứa 'token'")]
    public async Task LoginOverHttp_IsNotProcessed()
    {
        using var f = new HotelApiFactory();
        using var https = f.Https();
        var (email, _) = await https.RegisterAndLogin();                  // đối chứng: thông tin đăng nhập đúng

        using var http = f.Http();
        var r = await http.PostAsJsonAsync("/api/sessions", new { identifier = email, password = ApiHelpers.Password });

        Assert.Equal(HttpStatusCode.TemporaryRedirect, r.StatusCode);
        Assert.DoesNotContain("token", await r.Content.ReadAsStringAsync());
    }

    [TcFact("TC-API-TLS-003", "Gọi qua HTTPS không bị redirect (không vòng lặp chuyển hướng)",
        Steps = "POST /api/sessions với dữ liệu sai định dạng qua https://localhost",
        Expected = "400 (do logic nghiệp vụ), KHÔNG phải 307")]
    public async Task Https_IsNotRedirected()
    {
        using var f = new HotelApiFactory();
        using var c = f.Https();

        var r = await c.Login("khong-hop-le", "x");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Null(r.Headers.Location);
    }

    [TcFact("TC-API-TLS-004", "Môi trường Production gửi header HSTS khi truy cập HTTPS (host không phải localhost)",
        Pre = "ASPNETCORE_ENVIRONMENT=Production", Steps = "GET https://hotel.example/api/customer",
        Expected = "Response có header Strict-Transport-Security chứa max-age=")]
    public async Task Production_SendsHstsHeader()
    {
        using var f = new HotelApiFactory { EnvName = "Production" };
        using var c = f.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://hotel.example"), AllowAutoRedirect = false
        });

        var r = await c.GetAsync("/api/customer");

        Assert.True(r.Headers.Contains("Strict-Transport-Security"), "Thiếu header HSTS");
        Assert.Contains("max-age=", string.Join(";", r.Headers.GetValues("Strict-Transport-Security")));
    }
}
