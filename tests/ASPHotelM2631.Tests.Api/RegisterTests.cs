using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

public class RegisterTests : IDisposable
{
    private readonly HotelApiFactory _f = new();
    private readonly HttpClient _c;
    public RegisterTests() { _c = _f.Https(); }
    public void Dispose() { _c.Dispose(); _f.Dispose(); }

    [TcFact("TC-API-REG-001", "POST /api/users bằng email hợp lệ -> 201 và thông báo thành công", Priority = TcMeta.High,
        Pre = "DB đã có đủ 6 role", Steps = "POST /api/users {fullName, identifier=email, password}",
        Expected = "HTTP 201; body.message = 'Đăng ký thành công.'; body không chứa mật khẩu")]
    public async Task Register_Email_Returns201()
    {
        var r = await _c.Register("Nguyen Van A", ApiHelpers.NewEmail(), ApiHelpers.Password);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("Đăng ký thành công.", body);
        Assert.DoesNotContain(ApiHelpers.Password, body);
    }

    [TcFact("TC-API-REG-002", "POST /api/users bằng SĐT hợp lệ (có dấu cách) -> 201",
        Steps = "POST /api/users identifier='0912 345 678'", Expected = "HTTP 201")]
    public async Task Register_Phone_Returns201()
    {
        var phone = "09" + Random.Shared.Next(10_000_000, 99_999_999);   // 10 số
        var r = await _c.Register("Nguyen Van B", phone.Insert(4, " "), ApiHelpers.Password);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [TcFact("TC-API-REG-003", "Đăng ký trùng tài khoản -> 409; trùng cả khi khác hoa/thường", Priority = TcMeta.High,
        Steps = "Đăng ký email X (201) | Đăng ký lại X | Đăng ký lại X viết HOA",
        Expected = "Lần 1: 201 (đối chứng). Lần 2 và 3: 409 + message 'đã được đăng ký'")]
    public async Task Register_Duplicate_Returns409()
    {
        var email = ApiHelpers.NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await _c.Register("A", email, ApiHelpers.Password)).StatusCode); // đối chứng

        var again = await _c.Register("A", email, ApiHelpers.Password);
        var upper = await _c.Register("A", email.ToUpperInvariant(), ApiHelpers.Password);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Contains("đã được đăng ký", await again.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, upper.StatusCode);
    }

    [TcTheory("TC-API-REG-004", "POST /api/users dữ liệu không hợp lệ -> 400", Priority = TcMeta.High,
        Steps = "POST /api/users với <name>,<identifier>,<password> sai", Expected = "HTTP 400; message 'Thông tin đăng ký không hợp lệ.'")]
    [InlineData("", "ok@example.com", "Abcd1234")]
    [InlineData("Ten", "khong-hop-le", "Abcd1234")]
    [InlineData("Ten", "ok@example.com", "abcdefgh")]
    [InlineData("Ten", "ok@example.com", "abc123")]
    public async Task Register_Invalid_Returns400(string name, string id, string pw)
    {
        var r = await _c.Register(name, id, pw);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [TcTheory("TC-API-REG-005", "POST /api/users body thiếu/hỏng -> 400 (không 500)",
        Steps = "POST body: '{}' / JSON hỏng / chuỗi rỗng, Content-Type json", Expected = "HTTP 400")]
    [InlineData("{}")]
    [InlineData("{ broken json")]
    [InlineData("")]
    public async Task Register_MalformedBody_Returns400(string body)
    {
        var r = await _c.PostAsync("/api/users", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [TcFact("TC-API-REG-006", "POST /api/users sai Content-Type -> 415",
        Steps = "POST body JSON nhưng Content-Type text/plain", Expected = "HTTP 415")]
    public async Task Register_WrongContentType_Returns415()
    {
        var r = await _c.PostAsync("/api/users",
            new StringContent("{\"fullName\":\"a\",\"identifier\":\"a@b.co\",\"password\":\"Abcd1234\"}", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, r.StatusCode);
    }
}

/// <summary>Mô phỏng đúng trạng thái DB vừa migrate xong: CHƯA có role nào (Program.cs đang comment dòng seeder).</summary>
public class RegisterFreshDatabaseTests
{
    [TcFact("TC-API-REG-008", "Đăng ký trên DB mới migrate (chưa seed role) vẫn phải trả 201, không 500", Priority = TcMeta.High,
        Pre = "DB trống, chưa seed role KhachHang", Steps = "POST /api/users hợp lệ",
        Expected = "HTTP 201 (hoặc ứng dụng tự tạo role khi thiếu). KHÔNG được 500 và không để lại user mồ côi")]
    public async Task Register_OnDatabaseWithoutRoles_DoesNotCrash()
    {
        using var f = new HotelApiFactory { SeedRoles = false };
        using var c = f.Https();

        var email = ApiHelpers.NewEmail();
        var r = await c.Register("Fresh", email, ApiHelpers.Password);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }
}
