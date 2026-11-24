using ASPHotelM2631.Application.Authentication;

namespace ASPHotelM2631.Tests.Unit.Fakes;

/// <summary>
/// Fake viết tay (không dùng mock framework) để GHI LẠI mọi lần gọi.
/// Mặc định trả về giá trị "lạ" (LockedOut / token sentinel) để test nào quên set kết quả
/// sẽ lộ ra ngay thay vì pass nhầm bằng giá trị mặc định.
/// </summary>
public sealed class FakeAccountStore : IAccountStore
{
    public AuthenticationStatus CreateResult { get; set; } = AuthenticationStatus.LockedOut;
    public LoginResult SignInResult { get; set; } = new(AuthenticationStatus.LockedOut, Token: "SENTINEL");

    public List<(string FullName, string Identifier, string Password)> CreateCalls { get; } = new();
    public List<(string Identifier, string Password)> SignInCalls { get; } = new();
    public int SignOutCalls { get; private set; }

    public Task<AuthenticationStatus> CreateAsync(string fullName, string identifier, string password)
    {
        CreateCalls.Add((fullName, identifier, password));
        return Task.FromResult(CreateResult);
    }

    public Task<LoginResult> SignInAsync(string identifier, string password)
    {
        SignInCalls.Add((identifier, password));
        return Task.FromResult(SignInResult);
    }

    public Task SignOutAsync() { SignOutCalls++; return Task.CompletedTask; }
}
