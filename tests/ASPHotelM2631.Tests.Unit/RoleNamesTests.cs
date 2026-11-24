using ASPHotelM2631.Domain.Constants;
using Xunit;

namespace ASPHotelM2631.Tests.Unit;

public class RoleNamesTests
{
    [TcFact("TC-ROLE-001", "RoleNames.All chứa đủ 6 vai trò, không trùng, đúng giá trị chuỗi (đang là khoá trong DB)", Priority = TcMeta.High,
        Steps = "Đọc RoleNames.All", Expected = "Đúng 6 giá trị: KhachHang, LeTan, BuongPhong, KeToan, Manager, Admin")]
    public void All_ContainsExactlySixDistinctRoles()
    {
        var expected = new[] { "KhachHang", "LeTan", "BuongPhong", "KeToan", "Manager", "Admin" };

        Assert.Equal(6, RoleNames.All.Length);
        Assert.Equal(6, RoleNames.All.Distinct().Count());
        foreach (var role in expected) Assert.Contains(role, RoleNames.All);
    }
}
