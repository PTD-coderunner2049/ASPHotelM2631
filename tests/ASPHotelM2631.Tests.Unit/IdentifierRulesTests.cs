using ASPHotelM2631.Application.Authentication;
using Xunit;

namespace ASPHotelM2631.Tests.Unit;

public class IdentifierRulesTests
{
    // ---------- Normalize ----------
    [TcTheory("TC-ID-001", "Normalize: email được trim và hạ chữ thường", Priority = TcMeta.High,
        Steps = "Gọi Normalize(<input>)", Expected = "Trả đúng chuỗi chuẩn hoá (thường, không khoảng trắng đầu/cuối)")]
    [InlineData("  A.B@Example.COM  ", "a.b@example.com")]
    [InlineData("USER@HOTEL.VN", "user@hotel.vn")]
    public void Normalize_Email(string raw, string expected) =>
        Assert.Equal(expected, IdentifierRules.Normalize(raw));

    [TcTheory("TC-ID-002", "Normalize: SĐT bỏ khoảng trắng, dấu chấm, gạch ngang", Priority = TcMeta.High,
        Steps = "Gọi Normalize(<input>)", Expected = "Chỉ còn chữ số (và '+' đầu nếu có)")]
    [InlineData("0912 345 678", "0912345678")]
    [InlineData("0912-345-678", "0912345678")]
    [InlineData("0912.345.678", "0912345678")]
    [InlineData("+84 912 345 678", "+84912345678")]
    public void Normalize_Phone(string raw, string expected) =>
        Assert.Equal(expected, IdentifierRules.Normalize(raw));

    [TcTheory("TC-ID-003", "Normalize: null/rỗng trả về chuỗi rỗng, không ném lỗi",
        Steps = "Gọi Normalize(null / '' / '   ')", Expected = "Trả \"\"")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrBlank(string? raw) =>
        Assert.Equal("", IdentifierRules.Normalize(raw));

    // ---------- IsValid ----------
    [TcTheory("TC-ID-004", "IsValid: chấp nhận email và SĐT VN đúng định dạng", Priority = TcMeta.High,
        Steps = "Gọi IsValid(<input>)", Expected = "true")]
    [InlineData("a@b.co")]
    [InlineData("nguyen.van.a@khachsan.com.vn")]
    [InlineData("0912345678")]
    [InlineData("+84912345678")]
    public void IsValid_Accepts(string id) => Assert.True(IdentifierRules.IsValid(id));

    [TcTheory("TC-ID-005", "IsValid: từ chối định dạng sai (biên độ dài SĐT, thiếu @, thiếu tên miền...)", Priority = TcMeta.High,
        Steps = "Gọi IsValid(<input>)", Expected = "false")]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a@b")]            // thiếu .tld
    [InlineData("@b.com")]         // thiếu phần local
    [InlineData("a@@b.com")]
    [InlineData("a b@c.com")]      // khoảng trắng
    [InlineData("091234567")]      // 9 số  (thiếu 1)
    [InlineData("09123456789")]    // 11 số (thừa 1)
    [InlineData("1912345678")]     // không bắt đầu bằng 0 / +84
    [InlineData("+8409123456789")]
    [InlineData("091234567a")]
    public void IsValid_Rejects(string id) => Assert.False(IdentifierRules.IsValid(id));

    [TcFact("TC-ID-006", "Normalize rồi IsValid: email có khoảng trắng GIỮA vẫn bị từ chối (không bị 'dọn' thành hợp lệ)",
        Steps = "IsValid(Normalize('a b@c.com'))", Expected = "false")]
    public void Normalize_DoesNotRepairEmailWithInnerSpace() =>
        Assert.False(IdentifierRules.IsValid(IdentifierRules.Normalize("a b@c.com")));

    // ---------- IsStrongPassword ----------
    [TcTheory("TC-PW-001", "Mật khẩu hợp lệ: >= 8 ký tự, có chữ và số", Priority = TcMeta.High,
        Steps = "IsStrongPassword(<pw>)", Expected = "true")]
    [InlineData("abcdefg1")]       // đúng 8 ký tự (biên)
    [InlineData("Abcd1234")]
    [InlineData("12345678a")]
    [InlineData("Abcd1234!@#")]
    public void StrongPassword_Accepts(string pw) => Assert.True(IdentifierRules.IsStrongPassword(pw));

    [TcTheory("TC-PW-002", "Mật khẩu không hợp lệ: ngắn / thiếu chữ / thiếu số / null", Priority = TcMeta.High,
        Steps = "IsStrongPassword(<pw>)", Expected = "false")]
    [InlineData("abcdef1")]        // 7 ký tự (biên dưới - 1)
    [InlineData("abcdefgh")]       // thiếu số
    [InlineData("12345678")]       // thiếu chữ
    [InlineData("!!!!!!!!")]
    [InlineData("")]
    [InlineData(null)]
    public void StrongPassword_Rejects(string? pw) => Assert.False(IdentifierRules.IsStrongPassword(pw));
}
