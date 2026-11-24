using System.Net;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

/// <summary>
/// Test trên server THẬT (Kestrel + chứng chỉ dev). Không chạy trong `dotnet test` mặc định (Category=Live).
/// Chạy bằng tools/run-tests.ps1 -Live sau khi đã bật ứng dụng. Thiếu biến môi trường => FAIL (không skip),
/// vì test bị bỏ qua mà vẫn báo xanh chính là pass ảo.
///   LIVE_HTTP_URL  = http://localhost:5160     LIVE_HTTPS_URL = https://localhost:7069
/// </summary>
[Trait("Category", "Live")]
public class LiveHttpsTests
{
    private static string Env(string name) =>
        Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Chưa đặt biến môi trường {name}");

    [TcFact("TC-LIVE-TLS-001", "Server thật: HTTP -> 307 sang HTTPS đúng cổng", Priority = TcMeta.High,
        Pre = "Ứng dụng đang chạy profile https", Steps = "GET {LIVE_HTTP_URL}/api/customer (không theo redirect)",
        Expected = "307; Location bắt đầu bằng LIVE_HTTPS_URL")]
    public async Task RealServer_HttpRedirectsToHttps()
    {
        using var c = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var r = await c.GetAsync(Env("LIVE_HTTP_URL") + "/api/customer");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, r.StatusCode);
        Assert.StartsWith(Env("LIVE_HTTPS_URL"), r.Headers.Location!.ToString());
    }

    [TcFact("TC-LIVE-TLS-002", "Server thật: bắt tay TLS thành công với chứng chỉ được tin cậy (không tắt kiểm tra chứng chỉ)", Priority = TcMeta.High,
        Pre = "Đã chạy `dotnet dev-certs https --trust`", Steps = "GET {LIVE_HTTPS_URL}/api/customer bằng HttpClient mặc định",
        Expected = "Không ném HttpRequestException/AuthenticationException; trả 401 (cần đăng nhập)")]
    public async Task RealServer_TlsHandshakeSucceeds()
    {
        using var c = new HttpClient();     // cố ý KHÔNG đặt ServerCertificateCustomValidationCallback

        var r = await c.GetAsync(Env("LIVE_HTTPS_URL") + "/api/customer");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
