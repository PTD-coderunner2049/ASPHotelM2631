using Xunit;

// File dùng chung: các project test thêm bằng <Compile Include="..\Shared\TcAttributes.cs" />.
// MỖI test bắt buộc dùng [TcFact] hoặc [TcTheory]. Đây là NGUỒN DUY NHẤT của tài liệu:
// tools/Generate-TestDocs.py đọc các attribute này để sinh Test Case doc, và khớp ID với kết quả chạy (TRX).
// Quy ước ID:  TC-<NHÓM>-<SỐ 3 CHỮ SỐ>   ví dụ TC-REG-001, TC-API-LOGIN-004 (không đổi ID sau khi đã dùng).
namespace ASPHotelM2631.Tests;

public static class TcMeta
{
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
}

public sealed class TcFactAttribute : FactAttribute
{
    public TcFactAttribute(string id, string title) { DisplayName = $"{id} {title}"; }
    public string Req { get; set; } = "";          // mã yêu cầu (nếu nhóm có), vd FR-AUTH-01
    public string Priority { get; set; } = TcMeta.Medium;
    public string Pre { get; set; } = "";          // điều kiện tiên quyết
    public string Steps { get; set; } = "";        // các bước, ngăn cách bằng " | "
    public string Expected { get; set; } = "";     // kết quả mong đợi (cụ thể, kiểm chứng được)
}

public sealed class TcTheoryAttribute : TheoryAttribute
{
    public TcTheoryAttribute(string id, string title) { DisplayName = $"{id} {title}"; }
    public string Req { get; set; } = "";
    public string Priority { get; set; } = TcMeta.Medium;
    public string Pre { get; set; } = "";
    public string Steps { get; set; } = "";
    public string Expected { get; set; } = "";
}
