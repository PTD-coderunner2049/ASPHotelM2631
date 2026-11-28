namespace ASPHotelM2631.Application.Authentication;

public interface ITokenService
{
    /// <summary>Tạo token và refresh token.</summary>
    /// <param name="userId">Id của user.</param>
    /// <param name="role">Vai trò của user.</param>
    /// <returns>Token, refresh token và thời gian hết hạn.</returns>
    (string Token, DateTime ExpiresAt) CreateToken(
        string userId, string userName, string fullName,
        IEnumerable<string> roles, string securityStamp);
}