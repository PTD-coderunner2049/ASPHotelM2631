using System.Net;
using System.Text;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

public class LoginTests : IDisposable
{
    private readonly HotelApiFactory _f = new();
    private readonly HttpClient _c;
    public LoginTests() { _c = _f.Https(); }
    public void Dispose() { _c.Dispose(); _f.Dispose(); }

    private async Task<string> NewAccount()
    {
        var email = ApiHelpers.NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await _c.Register("Nguyen Van A", email, ApiHelpers.Password)).StatusCode);
        await _c.ConfirmEmail(email);
        return email;
    }

    [TcFact("TC-API-LOGIN-001", "POST /api/sessions đúng thông tin -> 201 kèm JWT, hạn dùng và thông tin user", Priority = TcMeta.High,
        Pre = "Đã đăng ký tài khoản", Steps = "POST /api/sessions {identifier, password}",
        Expected = "201; tokenType='Bearer'; token là JWT 3 đoạn; expiresAt ở tương lai; user.role='KhachHang'; user.fullName đúng; không lộ mật khẩu")]
    public async Task Login_Valid_Returns201WithToken()
    {
        var email = await NewAccount();

        var r = await _c.Login(email, ApiHelpers.Password);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var raw = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ApiHelpers.Password, raw);
        var json = await r.Json();
        Assert.Equal("Bearer", json.GetProperty("tokenType").GetString());
        Assert.Equal(3, json.GetProperty("token").GetString()!.Split('.').Length);
        Assert.True(json.GetProperty("expiresAt").GetDateTime() > DateTime.UtcNow);
        Assert.Equal("KhachHang", json.GetProperty("user").GetProperty("role").GetString());
        Assert.Equal("Nguyen Van A", json.GetProperty("user").GetProperty("fullName").GetString());
    }

    [TcFact("TC-API-LOGIN-002", "Sai mật khẩu và tài khoản không tồn tại trả CÙNG một thông báo 401 (không lộ tài khoản có tồn tại)", Priority = TcMeta.High,
        Steps = "Login sai mật khẩu | Login tài khoản không tồn tại",
        Expected = "Cả hai 401, body.message giống hệt nhau, không có token")]
    public async Task Login_WrongPasswordAndUnknownUser_SameResponse()
    {
        var email = await NewAccount();

        var wrongPw = await _c.Login(email, "SaiMatKhau1");
        var unknown = await _c.Login(ApiHelpers.NewEmail(), ApiHelpers.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPw.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        var m1 = (await wrongPw.Json()).GetProperty("message").GetString();
        var m2 = (await unknown.Json()).GetProperty("message").GetString();
        Assert.Equal("Email/số điện thoại hoặc mật khẩu không đúng.", m1);
        Assert.Equal(m1, m2);
    }

    [TcTheory("TC-API-LOGIN-003", "POST /api/sessions dữ liệu sai định dạng -> 400",
        Steps = "POST /api/sessions với <identifier>,<password>", Expected = "HTTP 400")]
    [InlineData("khong-hop-le", "Abcd1234")]
    [InlineData("a@b.co", "")]
    public async Task Login_InvalidInput_Returns400(string id, string pw)
    {
        var r = await _c.Login(id, pw);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [TcFact("TC-API-LOGIN-004", "Sai mật khẩu 5 lần -> khóa (423); khi đang khóa, mật khẩu ĐÚNG cũng bị từ chối", Priority = TcMeta.High,
        Pre = "Tài khoản mới", Steps = "Login sai x4 | Login sai lần 5 | Login đúng mật khẩu",
        Expected = "4 lần đầu 401; lần 5 = 423; lần đúng mật khẩu = 423, không có token")]
    public async Task Login_FiveWrongAttempts_LocksAccount()
    {
        var email = await NewAccount();

        for (var i = 1; i <= 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await _c.Login(email, $"Sai{i}Matkhau")).StatusCode);

        Assert.Equal((HttpStatusCode)423, (await _c.Login(email, "Sai5Matkhau")).StatusCode);

        var withCorrect = await _c.Login(email, ApiHelpers.Password);
        Assert.Equal((HttpStatusCode)423, withCorrect.StatusCode);
        Assert.DoesNotContain("token", await withCorrect.Content.ReadAsStringAsync());
    }

    [TcFact("TC-API-LOGIN-005", "Đăng nhập không phân biệt hoa/thường với email, bỏ dấu phân cách với SĐT",
        Steps = "Đăng ký email thường -> login bằng email HOA | Đăng ký SĐT -> login bằng SĐT có dấu '-'",
        Expected = "Cả hai 201")]
    public async Task Login_IdentifierIsNormalized()
    {
        var email = await NewAccount();
        Assert.Equal(HttpStatusCode.Created, (await _c.Login(email.ToUpperInvariant(), ApiHelpers.Password)).StatusCode);

        var phone = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        Assert.Equal(HttpStatusCode.Created, (await _c.Register("B", phone, ApiHelpers.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _c.Login(phone.Insert(4, "-"), ApiHelpers.Password)).StatusCode);
    }

    [TcFact("TC-API-LOGIN-006", "Lockout của một tài khoản không ảnh hưởng tài khoản khác",
        Steps = "Khóa tài khoản A | Login tài khoản B đúng mật khẩu", Expected = "B vẫn 201")]
    public async Task Login_LockoutIsPerAccount()
    {
        var a = await NewAccount();
        var b = await NewAccount();
        for (var i = 0; i < 5; i++) await _c.Login(a, "SaiMatKhau1");
        Assert.Equal((HttpStatusCode)423, (await _c.Login(a, ApiHelpers.Password)).StatusCode);   // đối chứng: A đã bị khóa

        Assert.Equal(HttpStatusCode.Created, (await _c.Login(b, ApiHelpers.Password)).StatusCode);
    }
}
