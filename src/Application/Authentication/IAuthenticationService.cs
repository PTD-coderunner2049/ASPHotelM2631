namespace HotelManagement.Application.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationStatus> RegisterAsync(string fullName, string identifier, string password);
    Task<AuthenticationStatus> SignInAsync(string identifier, string password);
    Task SignOutAsync();
}
