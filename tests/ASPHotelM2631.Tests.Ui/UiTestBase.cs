using System.Reflection;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace ASPHotelM2631.Tests.Ui;

/// <summary>
/// Nền cho mọi test giao diện. Mục tiêu: KHÔNG thể có test UI "pass ảo".
///  - Mặc định chạy có cửa sổ (headed) và dừng UI_PACE_MS giữa các bước để người test XEM được nó làm gì.
///  - Mọi bước chụp ảnh vào TestResults/evidence/&lt;TC-ID&gt;/ và ghi steps.log. Không có bằng chứng = không hợp lệ
///    (tools/Generate-TestDocs.py đánh FAIL các TC UI thiếu ảnh hoặc chưa có chữ ký xác nhận trong docs/testing/ui-signoff.csv).
///  - KHÔNG dùng --ignore-certificate-errors: lỗi chứng chỉ HTTPS phải làm test fail, không được che đi.
///  - Phát hiện trang lỗi của trình duyệt (không kết nối được...) và fail, tránh trường hợp assert "không có exception" mà thực ra đang nhìn trang lỗi.
/// Biến môi trường: UI_BASE_URL (https://localhost:7069), UI_HTTP_URL (http://localhost:5160), HEADLESS=1, UI_PACE_MS (700), EVIDENCE_DIR.
/// </summary>
public abstract class UiTestBase : IDisposable
{
    public static string BaseUrl => (Environment.GetEnvironmentVariable("UI_BASE_URL") ?? "https://localhost:7069").TrimEnd('/');
    public static string HttpUrl => (Environment.GetEnvironmentVariable("UI_HTTP_URL") ?? "http://localhost:5160").TrimEnd('/');

    private readonly string _evidenceDir;
    private readonly int _paceMs;
    private int _step;

    protected IWebDriver Driver { get; }
    protected WebDriverWait Wait { get; }
    protected string TcId { get; }

    protected UiTestBase(ITestOutputHelper output)
    {
        TcId = ReadTcId(output);
        var root = Environment.GetEnvironmentVariable("EVIDENCE_DIR") ?? Path.Combine("TestResults", "evidence");
        _evidenceDir = Path.GetFullPath(Path.Combine(root, TcId));
        if (Directory.Exists(_evidenceDir)) Directory.Delete(_evidenceDir, recursive: true);   // chạy lại = bằng chứng mới
        Directory.CreateDirectory(_evidenceDir);

        var headless = Environment.GetEnvironmentVariable("HEADLESS") == "1";
        _paceMs = headless ? 0 : int.TryParse(Environment.GetEnvironmentVariable("UI_PACE_MS"), out var p) ? p : 700;

        var options = new ChromeOptions();
        if (headless) options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1280,900");
        Driver = new ChromeDriver(options);          // Selenium Manager tự tải chromedriver
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
    }

    /// <summary>Đi tới URL, kiểm tra không phải trang lỗi trình duyệt, chụp bằng chứng.</summary>
    protected void Open(string url, string stepName = "open")
    {
        Driver.Navigate().GoToUrl(url);
        Step(stepName);
    }

    /// <summary>Gọi sau mỗi hành động: dừng cho người xem, kiểm tra trang lỗi, chụp ảnh, ghi log.</summary>
    protected void Step(string name)
    {
        if (_paceMs > 0) Thread.Sleep(_paceMs);
        _step++;
        var file = $"{_step:00}-{Regex.Replace(name, @"[^\w\-]+", "_")}.png";
        ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(Path.Combine(_evidenceDir, file));
        File.AppendAllText(Path.Combine(_evidenceDir, "steps.log"),
            $"{DateTime.Now:HH:mm:ss}\t{file}\t{Driver.Url}\t{Driver.Title}{Environment.NewLine}");

        if (Driver.FindElements(By.Id("main-frame-error")).Count > 0)
            throw new InvalidOperationException($"Trình duyệt hiển thị trang lỗi tại bước '{name}' ({Driver.Url}). Xem {file}.");
    }

    public void Dispose()
    {
        try { Step("final"); } catch { /* ảnh cuối chỉ để làm bằng chứng */ }
        Driver.Quit();
    }

    // xunit v2 không đưa tên test cho constructor; lấy từ ITestOutputHelper để ID chỉ khai báo MỘT chỗ ([TcFact]).
    private static string ReadTcId(ITestOutputHelper output)
    {
        var test = output.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(output) as ITest
                   ?? throw new InvalidOperationException("Không đọc được tên test từ ITestOutputHelper.");
        var m = Regex.Match(test.DisplayName, @"^TC-[A-Z0-9\-]+");
        return m.Success ? m.Value : throw new InvalidOperationException($"Test '{test.DisplayName}' không dùng [TcFact]/[TcTheory].");
    }
}
