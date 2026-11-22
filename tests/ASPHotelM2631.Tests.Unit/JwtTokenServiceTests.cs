using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ASPHotelM2631.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ASPHotelM2631.Tests.Unit;

public class JwtTokenServiceTests
{
    private const string Key = "1234567890qwertyuiopasdfghjklzxcv";

    private static JwtTokenService Make(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "iss", ["Jwt:Audience"] = "aud", ["Jwt:ExpireMinutes"] = "15"
        };
        if (overrides is not null) foreach (var kv in overrides) values[kv.Key] = kv.Value;
        return new JwtTokenService(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [TcFact("TC-JWT-001", "Token chứa đủ claim sub/name/fullName/securityStamp/role, issuer và audience đúng", Priority = TcMeta.High,
        Steps = "CreateToken('u1','user','Ten Day Du',['KhachHang'],'stamp-1')",
        Expected = "Các claim đúng giá trị; iss='iss'; aud='aud'")]
    public void CreateToken_ContainsExpectedClaims()
    {
        var (token, _) = Make().CreateToken("u1", "user", "Ten Day Du", new[] { "KhachHang" }, "stamp-1");
        var jwt = Read(token);

        Assert.Equal("u1", jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("user", jwt.Claims.Single(c => c.Type == "name").Value);
        Assert.Equal("Ten Day Du", jwt.Claims.Single(c => c.Type == "fullName").Value);
        Assert.Equal("stamp-1", jwt.Claims.Single(c => c.Type == "securityStamp").Value);
        Assert.Equal("KhachHang", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("iss", jwt.Issuer);
        Assert.Contains("aud", jwt.Audiences);
    }

    [TcFact("TC-JWT-002", "Nhiều vai trò -> nhiều claim role",
        Steps = "CreateToken(..., roles=['LeTan','Manager'], ...)", Expected = "Có đúng 2 claim role: LeTan và Manager")]
    public void CreateToken_MultipleRoles()
    {
        var (token, _) = Make().CreateToken("u1", "user", "T", new[] { "LeTan", "Manager" }, "s");

        var roles = Read(token).Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("LeTan", roles);
        Assert.Contains("Manager", roles);
    }

    [TcTheory("TC-JWT-003", "Thời hạn token theo Jwt:ExpireMinutes; thiếu/sai -> mặc định 15 phút",
        Steps = "CreateToken với ExpireMinutes=<cfg>", Expected = "ExpiresAt ~ now + <phút> (sai số 1 phút) và khớp claim exp")]
    [InlineData("30", 30)]
    [InlineData("5", 5)]
    [InlineData(null, 15)]
    [InlineData("abc", 15)]
    public void CreateToken_Expiry(string? cfg, int expectedMinutes)
    {
        var (token, expiresAt) = Make(new() { ["Jwt:ExpireMinutes"] = cfg }).CreateToken("u", "n", "f", new[] { "r" }, "s");

        Assert.InRange((expiresAt - DateTime.UtcNow).TotalMinutes, expectedMinutes - 1, expectedMinutes);
        Assert.InRange(Math.Abs((Read(token).ValidTo - expiresAt).TotalSeconds), 0, 2);
    }

    [TcFact("TC-JWT-004", "Chữ ký hợp lệ với đúng key và bị từ chối với key khác", Priority = TcMeta.High,
        Steps = "Validate token bằng key đúng, rồi bằng key sai", Expected = "Key đúng: hợp lệ; key sai: SecurityTokenSignatureKeyNotFound/InvalidSignature")]
    public void CreateToken_SignatureBoundToKey()
    {
        var (token, _) = Make().CreateToken("u", "n", "f", new[] { "r" }, "s");
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        TokenValidationParameters P(string key) => new()
        {
            ValidIssuer = "iss", ValidAudience = "aud", ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        handler.ValidateToken(token, P(Key), out _);                                   // key đúng: không ném lỗi
        Assert.ThrowsAny<SecurityTokenException>(
            () => handler.ValidateToken(token, P("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ"), out _)); // key sai: phải ném lỗi
    }

    [TcFact("TC-JWT-005", "Thiếu Jwt:Key -> ném InvalidOperationException (fail-fast, không ký bằng key rỗng)",
        Steps = "Cấu hình không có Jwt:Key; CreateToken", Expected = "InvalidOperationException")]
    public void CreateToken_MissingKey_Throws() =>
        Assert.Throws<InvalidOperationException>(
            () => Make(new() { ["Jwt:Key"] = null }).CreateToken("u", "n", "f", new[] { "r" }, "s"));
}
