namespace ASPHotelM2631.Application.Authentication;

public sealed class AuthenticationService(IAccountStore store) : IAuthenticationService
{
    public Task<AuthenticationStatus> RegisterAsync(string fullName, string identifier, string password)
    {
        var name = (fullName ?? "").Trim();
        var id = IdentifierRules.Normalize(identifier);

        if (name.Length is 0 or > 100 || !IdentifierRules.IsValid(id) ||
            !IdentifierRules.IsStrongPassword(password))
            return Task.FromResult(AuthenticationStatus.InvalidInput);

        return store.CreateAsync(name, id, password);
    }

    public Task<LoginResult> SignInAsync(string identifier, string password)
    {
        var id = IdentifierRules.Normalize(identifier);
        if (!IdentifierRules.IsValid(id) || string.IsNullOrEmpty(password))
            return Task.FromResult<LoginResult>(AuthenticationStatus.InvalidInput);

        return store.SignInAsync(id, password);
    }

    public Task SignOutAsync() => store.SignOutAsync();
}