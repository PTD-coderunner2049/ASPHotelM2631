using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace ASPHotelM2631.Tests.Ui;

/// <summary>
/// Giao diện hiện tại mới chỉ là template MVC (Home, Privacy). Test ở đây dựng sẵn đường ống bằng chứng;
/// khi có trang đăng nhập/đặt phòng, thêm Page Object + TC mới theo cùng mẫu (mỗi bước gọi Step()).
/// Assert luôn dựa trên nội dung THẤY được trên trang (h1, tiêu đề, URL cuối), không chỉ "không lỗi".
/// </summary>
[Trait("Category", "UI")]
public class HomePageTests(ITestOutputHelper output) : UiTestBase(output)
{
    private string H1() => Wait.Until(d => d.FindElement(By.TagName("h1"))).Text.Trim();

    [TcFact("TC-UI-HOME-001", "Trang chủ mở được qua HTTPS, hiển thị tiêu đề và thanh điều hướng", Priority = TcMeta.High,
        Pre = "Ứng dụng đang chạy ở UI_BASE_URL (HTTPS), chứng chỉ dev đã được tin cậy",
        Steps = "Mở {UI_BASE_URL}/ | Quan sát trang",
        Expected = "Title 'Home Page - ASPHotelM2631.Web'; h1 'Welcome'; navbar có liên kết 'Home' và 'Privacy'; URL bắt đầu bằng https://")]
    public void Home_LoadsOverHttps()
    {
        Open(BaseUrl + "/");

        Assert.StartsWith("https://", Driver.Url);
        Assert.Equal("Home Page - ASPHotelM2631.Web", Driver.Title);
        Assert.Equal("Welcome", H1());
        Assert.True(Driver.FindElement(By.LinkText("Home")).Displayed);
        Assert.True(Driver.FindElement(By.LinkText("Privacy")).Displayed);
    }

    [TcFact("TC-UI-HOME-002", "Bấm 'Privacy' trên thanh điều hướng chuyển sang trang Privacy Policy", Priority = TcMeta.High,
        Steps = "Mở trang chủ | Bấm liên kết 'Privacy' trên navbar | Quan sát trang",
        Expected = "URL kết thúc bằng /Home/Privacy; Title 'Privacy Policy - ASPHotelM2631.Web'; h1 'Privacy Policy'")]
    public void Navbar_PrivacyLink_Navigates()
    {
        Open(BaseUrl + "/");
        Assert.Equal("Welcome", H1());                       // đối chứng: đang ở trang chủ trước khi bấm

        Driver.FindElement(By.CssSelector("nav a.nav-link[href$='/Home/Privacy']")).Click();
        Wait.Until(d => d.Url.EndsWith("/Home/Privacy"));
        Step("after-click-privacy");

        Assert.Equal("Privacy Policy - ASPHotelM2631.Web", Driver.Title);
        Assert.Equal("Privacy Policy", H1());
    }

    [TcFact("TC-UI-HOME-003", "Logo/brand đưa người dùng từ Privacy về trang chủ",
        Steps = "Mở /Home/Privacy | Bấm brand 'ASPHotelM2631.Web' | Quan sát", Expected = "h1 'Welcome'")]
    public void Brand_ReturnsToHome()
    {
        Open(BaseUrl + "/Home/Privacy");
        Assert.Equal("Privacy Policy", H1());                // đối chứng

        Driver.FindElement(By.CssSelector("a.navbar-brand")).Click();
        Wait.Until(d => d.FindElements(By.TagName("h1")).Any(h => h.Text.Trim() == "Welcome"));
        Step("after-click-brand");

        Assert.Equal("Welcome", H1());
    }

    [TcFact("TC-UI-HOME-004", "Truy cập bằng HTTP, trình duyệt kết thúc ở HTTPS và trang hiển thị bình thường", Priority = TcMeta.High,
        Pre = "UI_HTTP_URL trỏ cổng HTTP của ứng dụng", Steps = "Mở {UI_HTTP_URL}/ | Quan sát URL cuối cùng",
        Expected = "URL cuối bắt đầu bằng https://; h1 'Welcome'")]
    public void Http_EndsOnHttps()
    {
        Open(HttpUrl + "/");
        Wait.Until(d => d.Url.StartsWith("https://"));

        Assert.StartsWith("https://", Driver.Url);
        Assert.Equal("Welcome", H1());
    }

    [TcFact("TC-UI-HOME-005", "Màn hình hẹp: menu thu gọn, bấm nút toggler mới hiện liên kết Privacy",
        Steps = "Đặt cửa sổ 500x800 | Mở trang chủ | Quan sát (Privacy ẩn) | Bấm toggler | Quan sát (Privacy hiện)",
        Expected = "Trước khi bấm: toggler hiện, link Privacy trên navbar KHÔNG hiện. Sau khi bấm: link Privacy hiện")]
    public void Navbar_CollapsesOnNarrowScreen()
    {
        Driver.Manage().Window.Size = new System.Drawing.Size(500, 800);
        Open(BaseUrl + "/", "narrow-before");
        var privacy = Driver.FindElement(By.CssSelector("nav a.nav-link[href$='/Home/Privacy']"));

        Assert.True(Driver.FindElement(By.CssSelector("button.navbar-toggler")).Displayed);
        Assert.False(privacy.Displayed);                     // đối chứng: ban đầu đang ẩn

        Driver.FindElement(By.CssSelector("button.navbar-toggler")).Click();
        Wait.Until(_ => privacy.Displayed);
        Step("narrow-after-toggle");

        Assert.True(privacy.Displayed);
    }
}
