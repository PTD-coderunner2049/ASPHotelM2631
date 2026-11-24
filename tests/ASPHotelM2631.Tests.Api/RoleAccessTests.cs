using System.Net;
using ASPHotelM2631.Domain.Constants;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

public class RoleAccessTests : IDisposable
{
    private readonly HotelApiFactory _f = new();
    private readonly HttpClient _c;
    public RoleAccessTests() { _c = _f.Https(); }
    public void Dispose() { _c.Dispose(); _f.Dispose(); }

    // endpoint -> role duy nhất được phép
    private static readonly (string Url, string Role)[] Routes =
    {
        ("/api/customer",     RoleNames.Customer),
        ("/api/reception",    RoleNames.Receptionist),
        ("/api/housekeeping", RoleNames.Housekeeping),
        ("/api/accounting",   RoleNames.Accountant),
        ("/api/manager",      RoleNames.Manager),
        ("/api/admin",        RoleNames.Admin),
    };

    public static IEnumerable<object[]> Urls() => Routes.Select(r => new object[] { r.Url });

    [TcTheory("TC-API-ROLE-001", "Các endpoint phân quyền yêu cầu đăng nhập: không token -> 401", Priority = TcMeta.High,
        Steps = "GET <endpoint> không Authorization", Expected = "401 (không phải 200 hay 403)")]
    [MemberData(nameof(Urls))]
    public async Task Endpoint_WithoutToken_Returns401(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Get(url))).StatusCode);
    }
}
