using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ASPHotelM2631.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

public static class ApiHelpers
{
    public const string Password = "Abcd1234";
    public static string NewEmail() => $"tc_{Guid.NewGuid():N}@example.com";

    /// <summary>Client HTTPS, KHÔNG tự theo redirect (để test được 307 thật).</summary>
    public static HttpClient Https(this HotelApiFactory f) => f.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    public static HttpClient Http(this HotelApiFactory f) => f.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
    });

    public static Task<HttpResponseMessage> Register(this HttpClient c, string name, string id, string pw) =>
        c.PostAsJsonAsync("/api/users", new { fullName = name, identifier = id, password = pw });

    public static Task<HttpResponseMessage> Login(this HttpClient c, string id, string pw) =>
        c.PostAsJsonAsync("/api/sessions", new { identifier = id, password = pw });

    public static async Task ConfirmEmail(this HttpClient c, string email)
    {
        Assert.True(CapturingEmailSender.TryTakeCode(email, out var code), "No email verification code was captured.");
        var response = await c.PostAsJsonAsync("/api/email-verifications/confirm", new { identifier = email, code });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task<JsonElement> Json(this HttpResponseMessage r)
    {
        var text = await r.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public static HttpRequestMessage Get(string url, string? token = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    public static HttpRequestMessage Delete(string url, string? token = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, url);
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    /// <summary>Đăng ký công khai rồi đăng nhập -> trả token. Fail ngay (đúng chỗ) nếu bước nào hỏng.</summary>
    public static async Task<(string Email, string Token)> RegisterAndLogin(this HttpClient c)
    {
        var email = NewEmail();
        var reg = await c.Register("Tester", email, Password);
        Assert.Equal(System.Net.HttpStatusCode.Created, reg.StatusCode);
        await c.ConfirmEmail(email);
        var login = await c.Login(email, Password);
        Assert.Equal(System.Net.HttpStatusCode.Created, login.StatusCode);
        return (email, (await login.Json()).GetProperty("token").GetString()!);
    }

    /// <summary>Tạo user với role bất kỳ trực tiếp qua UserManager (đăng ký công khai chỉ tạo được KhachHang), rồi đăng nhập qua API.</summary>
    public static async Task<string> TokenForRole(this HotelApiFactory f, HttpClient c, string role)
    {
        var email = NewEmail();
        using (var scope = f.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, FullName = $"User {role}", EmailConfirmed = true };
            (await users.CreateAsync(user, Password)).ThrowIfFailed();
            (await users.AddToRoleAsync(user, role)).ThrowIfFailed();
        }
        var login = await c.Login(email, Password);
        Assert.Equal(System.Net.HttpStatusCode.Created, login.StatusCode);
        return (await login.Json()).GetProperty("token").GetString()!;
    }

    /// <summary>Tự ký JWT với key tuỳ ý để mô phỏng token giả / hết hạn.</summary>
    public static string ForgeToken(string key, DateTime notBefore, DateTime expires, string stamp = "any-stamp")
    {
        var jwt = new JwtSecurityToken(
            issuer: "ASPHotelM2631", audience: "ASPHotelM2631.Clients",
            claims: new[] { new Claim("sub", "forged"), new Claim("name", "x"), new Claim("fullName", "x"),
                            new Claim("securityStamp", stamp), new Claim("role", "Admin") },
            notBefore: notBefore, expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
