namespace ASPHotelM2631.Application.Authentication;

public interface IAccountStore
{
    Task<AuthenticationStatus> CreateAsync(string fullName, string identifier, string password);
    Task<LoginResult> SignInAsync(string identifier, string password);
    Task SignOutAsync();
}