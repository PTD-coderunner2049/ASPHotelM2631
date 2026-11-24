namespace ASPHotelM2631.Application.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationStatus> RegisterAsync(string fullName, string identifier, string password);
    Task<LoginResult> SignInAsync(string identifier, string password);
    Task<AuthenticationStatus> SendVerificationCodeAsync(string identifier);
    Task<AuthenticationStatus> ConfirmEmailAsync(string identifier, string code);
    Task SignOutAsync();
}