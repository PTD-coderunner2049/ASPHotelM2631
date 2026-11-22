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

    public static IEnumerable<object[]> RoleByUrl() =>
        from role in RoleNames.All
        from route in Routes
        select new object[] { role, route.Url, route.Role == role ? 200 : 403 };

    [TcTheory("TC-API-ROLE-001", "Các endpoint phân quyền yêu cầu đăng nhập: không token -> 401", Priority = TcMeta.High,
        Steps = "GET <endpoint> không Authorization", Expected = "401 (không phải 200 hay 403)")]
    [MemberData(nameof(Urls))]
    public async Task Endpoint_WithoutToken_Returns401(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _c.SendAsync(ApiHelpers.Get(url))).StatusCode);
    }

    [TcTheory("TC-API-ROLE-002", "Ma trận phân quyền 6 vai trò x 6 endpoint: đúng vai trò -> 200, sai vai trò -> 403", Priority = TcMeta.High,
        Pre = "Có user thuộc từng vai trò", Steps = "Login bằng user <role> | GET <endpoint>",
        Expected = "200 nếu role khớp endpoint; ngược lại 403 (đã đăng nhập nhưng không đủ quyền)")]
    [MemberData(nameof(RoleByUrl))]
    public async Task RoleMatrix(string role, string url, int expectedStatus)
    {
        var token = await _f.TokenForRole(_c, role);

        var r = await _c.SendAsync(ApiHelpers.Get(url, token));

        Assert.Equal((HttpStatusCode)expectedStatus, r.StatusCode);
    }
}
