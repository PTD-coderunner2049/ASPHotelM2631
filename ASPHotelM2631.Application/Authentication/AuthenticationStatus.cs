namespace ASPHotelM2631.Application.Authentication;

public enum AuthenticationStatus
{
    Succeeded, InvalidInput, DuplicateAccount, InvalidCredentials, LockedOut,
    EmailNotConfirmed, TooManyRequests, InvalidCode
}

/// <summary>Kết quả đăng nhập kèm vai trò để API trả về giao diện.</summary>
public sealed record LoginResult(
    AuthenticationStatus Status,
    string? UserId = null,
    string? FullName = null,
    string? Role = null,
    string? Token = null,
    DateTime? ExpiresAt = null
    )
{
    // Cho phép viết gọn: return AuthenticationStatus.LockedOut;
    public static implicit operator LoginResult(AuthenticationStatus status) => new(status);
}