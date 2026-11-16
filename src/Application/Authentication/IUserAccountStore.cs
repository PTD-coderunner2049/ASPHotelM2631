using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.Authentication;

public interface IUserAccountStore
{
    Task<AuthenticationStatus> CreateAsync(UserAccount account, string password);
    Task<AuthenticationStatus> SignInAsync(string identifier, string password);
    Task SignOutAsync();
}
