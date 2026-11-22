using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Tests.Unit.Fakes;
using Xunit;

namespace ASPHotelM2631.Tests.Unit;

/// <summary>
/// Quy tắc chống pass ảo ở file này:
///  1) Case "hợp lệ" kiểm tra store nhận ĐÚNG tham số đã chuẩn hoá (không chỉ kiểm tra status).
///  2) Case "không hợp lệ" kiểm tra store KHÔNG bị gọi (CreateCalls/SignInCalls rỗng).
///  3) Fake trả giá trị sentinel nên không thể pass nhờ giá trị mặc định.
/// </summary>
public class AuthenticationServiceTests
{
    private static (AuthenticationService svc, FakeAccountStore store) Make()
    {
        var store = new FakeAccountStore();
        return (new AuthenticationService(store), store);
    }

    // ---------- Register ----------
    [TcFact("TC-REG-001", "Đăng ký hợp lệ: chuyển cho store dữ liệu đã chuẩn hoá, trả đúng kết quả của store", Priority = TcMeta.High,
        Steps = "RegisterAsync('  Nguyen Van A  ', '  A@Hotel.VN ', 'Abcd1234')",
        Expected = "Store.CreateAsync gọi đúng 1 lần với ('Nguyen Van A','a@hotel.vn','Abcd1234'); kết quả = kết quả store")]
    public async Task Register_Valid_PassesNormalizedDataToStore()
    {
        var (svc, store) = Make();
        store.CreateResult = AuthenticationStatus.Succeeded;

        var result = await svc.RegisterAsync("  Nguyen Van A  ", "  A@Hotel.VN ", "Abcd1234");

        Assert.Equal(AuthenticationStatus.Succeeded, result);
        var call = Assert.Single(store.CreateCalls);
        Assert.Equal("Nguyen Van A", call.FullName);
        Assert.Equal("a@hotel.vn", call.Identifier);
        Assert.Equal("Abcd1234", call.Password);
    }

    [TcFact("TC-REG-002", "Đăng ký bằng SĐT có dấu phân cách: store nhận SĐT đã bỏ dấu",
        Steps = "RegisterAsync('B', '0912 345-678', 'Abcd1234')", Expected = "Store nhận '0912345678'")]
    public async Task Register_Phone_IsNormalizedBeforeStore()
    {
        var (svc, store) = Make();
        store.CreateResult = AuthenticationStatus.Succeeded;

        await svc.RegisterAsync("B", "0912 345-678", "Abcd1234");

        Assert.Equal("0912345678", Assert.Single(store.CreateCalls).Identifier);
    }

    [TcTheory("TC-REG-003", "Đăng ký: dữ liệu sai bị chặn TRƯỚC khi chạm store", Priority = TcMeta.High,
        Steps = "RegisterAsync(<name>,<id>,<pw>)", Expected = "InvalidInput và Store.CreateAsync KHÔNG được gọi")]
    [InlineData("", "a@b.co", "Abcd1234")]                 // tên rỗng
    [InlineData("   ", "a@b.co", "Abcd1234")]              // tên toàn khoảng trắng
    [InlineData("Ten", "khong-hop-le", "Abcd1234")]        // định danh sai
    [InlineData("Ten", "a@b.co", "abcdefgh")]              // thiếu số
    [InlineData("Ten", "a@b.co", "abc123")]                // quá ngắn
    [InlineData("Ten", "", "Abcd1234")]
    public async Task Register_Invalid_NeverReachesStore(string name, string id, string pw)
    {
        var (svc, store) = Make();
        store.CreateResult = AuthenticationStatus.Succeeded;   // nếu bị gọi nhầm sẽ thành Succeeded -> test fail

        var result = await svc.RegisterAsync(name, id, pw);

        Assert.Equal(AuthenticationStatus.InvalidInput, result);
        Assert.Empty(store.CreateCalls);
    }

    [TcFact("TC-REG-004", "Đăng ký: tên dài 100 ký tự được chấp nhận, 101 ký tự bị từ chối (biên)",
        Steps = "RegisterAsync(100 ký tự) và RegisterAsync(101 ký tự)", Expected = "100 -> gọi store; 101 -> InvalidInput, không gọi store")]
    public async Task Register_NameLengthBoundary()
    {
        var (svc, store) = Make();
        store.CreateResult = AuthenticationStatus.Succeeded;

        var ok = await svc.RegisterAsync(new string('a', 100), "a@b.co", "Abcd1234");
        var tooLong = await svc.RegisterAsync(new string('a', 101), "a@b.co", "Abcd1234");

        Assert.Equal(AuthenticationStatus.Succeeded, ok);
        Assert.Equal(AuthenticationStatus.InvalidInput, tooLong);
        Assert.Single(store.CreateCalls);
    }

    [TcFact("TC-REG-005", "Đăng ký: null không làm service ném exception",
        Steps = "RegisterAsync(null, null, null)", Expected = "InvalidInput, không ném lỗi, không gọi store")]
    public async Task Register_Nulls_ReturnInvalidInput()
    {
        var (svc, store) = Make();

        var result = await svc.RegisterAsync(null!, null!, null!);

        Assert.Equal(AuthenticationStatus.InvalidInput, result);
        Assert.Empty(store.CreateCalls);
    }

    [TcTheory("TC-REG-006", "Đăng ký: trạng thái từ store (Duplicate...) được trả nguyên vẹn", Priority = TcMeta.High,
        Steps = "Store trả <status>; RegisterAsync(dữ liệu hợp lệ)", Expected = "Service trả đúng <status>")]
    [InlineData(AuthenticationStatus.DuplicateAccount)]
    [InlineData(AuthenticationStatus.InvalidInput)]
    [InlineData(AuthenticationStatus.Succeeded)]
    public async Task Register_PropagatesStoreStatus(AuthenticationStatus fromStore)
    {
        var (svc, store) = Make();
        store.CreateResult = fromStore;

        Assert.Equal(fromStore, await svc.RegisterAsync("Ten", "a@b.co", "Abcd1234"));
    }

    // ---------- SignIn ----------
    [TcFact("TC-LOGIN-001", "Đăng nhập hợp lệ: store nhận định danh đã chuẩn hoá, mật khẩu GIỮ NGUYÊN, kết quả trả nguyên vẹn", Priority = TcMeta.High,
        Steps = "SignInAsync('  A@Hotel.VN ', ' Abcd1234 ')",
        Expected = "Store.SignInAsync gọi 1 lần ('a@hotel.vn', ' Abcd1234 ' - không trim mật khẩu); trả đúng object của store")]
    public async Task SignIn_Valid_PassesNormalizedIdAndRawPassword()
    {
        var (svc, store) = Make();
        var expected = new LoginResult(AuthenticationStatus.Succeeded, "id-1", "Ten", "KhachHang", "tok", DateTime.UtcNow);
        store.SignInResult = expected;

        var result = await svc.SignInAsync("  A@Hotel.VN ", " Abcd1234 ");

        Assert.Same(expected, result);
        var call = Assert.Single(store.SignInCalls);
        Assert.Equal("a@hotel.vn", call.Identifier);
        Assert.Equal(" Abcd1234 ", call.Password);
    }

    [TcTheory("TC-LOGIN-002", "Đăng nhập: định danh sai hoặc mật khẩu rỗng bị chặn trước khi chạm store", Priority = TcMeta.High,
        Steps = "SignInAsync(<id>,<pw>)", Expected = "InvalidInput và Store.SignInAsync KHÔNG được gọi")]
    [InlineData("", "Abcd1234")]
    [InlineData("khong-hop-le", "Abcd1234")]
    [InlineData("a@b.co", "")]
    [InlineData("a@b.co", null)]
    [InlineData(null, "Abcd1234")]
    public async Task SignIn_Invalid_NeverReachesStore(string? id, string? pw)
    {
        var (svc, store) = Make();
        store.SignInResult = new LoginResult(AuthenticationStatus.Succeeded, Token: "leak");

        var result = await svc.SignInAsync(id!, pw!);

        Assert.Equal(AuthenticationStatus.InvalidInput, result.Status);
        Assert.Null(result.Token);
        Assert.Empty(store.SignInCalls);
    }

    [TcTheory("TC-LOGIN-003", "Đăng nhập: các trạng thái thất bại từ store (sai mật khẩu, khóa) được trả nguyên vẹn, không kèm token", Priority = TcMeta.High,
        Steps = "Store trả <status>; SignInAsync(hợp lệ)", Expected = "Status = <status>, Token = null")]
    [InlineData(AuthenticationStatus.InvalidCredentials)]
    [InlineData(AuthenticationStatus.LockedOut)]
    public async Task SignIn_PropagatesFailure(AuthenticationStatus fromStore)
    {
        var (svc, store) = Make();
        store.SignInResult = fromStore;           // implicit conversion -> LoginResult chỉ có Status

        var result = await svc.SignInAsync("a@b.co", "Abcd1234");

        Assert.Equal(fromStore, result.Status);
        Assert.Null(result.Token);
    }

    // ---------- SignOut ----------
    [TcFact("TC-LOGOUT-001", "Đăng xuất: service uỷ quyền cho store đúng 1 lần",
        Steps = "SignOutAsync()", Expected = "Store.SignOutAsync được gọi đúng 1 lần")]
    public async Task SignOut_DelegatesToStore()
    {
        var (svc, store) = Make();

        await svc.SignOutAsync();

        Assert.Equal(1, store.SignOutCalls);
    }
}
