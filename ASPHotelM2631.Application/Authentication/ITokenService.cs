namespace ASPHotelM2631.Application.Authentication;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(
        string userId, string userName, string fullName,
        IEnumerable<string> roles, string securityStamp);
}