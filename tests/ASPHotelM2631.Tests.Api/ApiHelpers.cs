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
        var login = await c.Login(email, Password);
        Assert.Equal(System.Net.HttpStatusCode.Created, login.StatusCode);
        return (email, (await login.Json()).GetProperty("token").GetString()!);
    }
}
