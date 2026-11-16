using System.Text.RegularExpressions;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.Authentication;

public sealed class AuthenticationService(IUserAccountStore accountStore) : IAuthenticationService
{
    private static readonly Regex EmailPattern = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PhoneSeparators = new(
        @"[\s.-]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PhonePattern = new(
        @"^(0|\+84)\d{9}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<AuthenticationStatus> RegisterAsync(string fullName, string identifier, string password)
    {
        var name = fullName.Trim();
        var normalizedIdentifier = NormalizeIdentifier(identifier);
        if (name.Length is 0 or > 100 ||
            normalizedIdentifier is null ||
            normalizedIdentifier.Length > (EmailPattern.IsMatch(normalizedIdentifier) ? 150 : 15) ||
            !IsValidPassword(password))
        {
            return AuthenticationStatus.InvalidInput;
        }

        var isEmail = EmailPattern.IsMatch(normalizedIdentifier);
        var account = new UserAccount(
            Guid.NewGuid(),
            name,
            normalizedIdentifier,
            isEmail ? normalizedIdentifier : null,
            isEmail ? null : normalizedIdentifier);

        return await accountStore.CreateAsync(account, password);
    }

    public Task<AuthenticationStatus> SignInAsync(string identifier, string password)
    {
        var normalizedIdentifier = NormalizeIdentifier(identifier);
        if (normalizedIdentifier is null ||
            normalizedIdentifier.Length > (EmailPattern.IsMatch(normalizedIdentifier) ? 150 : 15) ||
            string.IsNullOrEmpty(password))
        {
            return Task.FromResult(AuthenticationStatus.InvalidInput);
        }

        return accountStore.SignInAsync(normalizedIdentifier, password);
    }

    public Task SignOutAsync() => accountStore.SignOutAsync();

    private static string? NormalizeIdentifier(string identifier)
    {
        var value = identifier.Trim();
        if (EmailPattern.IsMatch(value))
        {
            return value.ToLowerInvariant();
        }

        var phone = PhoneSeparators.Replace(value, string.Empty);
        return PhonePattern.IsMatch(phone) ? phone : null;
    }

    private static bool IsValidPassword(string password) =>
        password.Length >= 8 &&
        password.Any(char.IsLetter) &&
        password.Any(char.IsDigit);
}
