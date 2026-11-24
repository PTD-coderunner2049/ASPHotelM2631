namespace ASPHotelM2631.Application.Authentication;

public sealed class AuthenticationService(IAccountStore store, IEmailVerification verification) : IAuthenticationService{
     public Task<AuthenticationStatus> SendVerificationCodeAsync(string identifier)
    {
        var id = IdentifierRules.Normalize(identifier);
        return id.Contains('@') && IdentifierRules.IsValid(id)
            ? verification.SendCodeAsync(id)
            : Task.FromResult(AuthenticationStatus.Succeeded);   // im lặng, không tiết lộ gì
    }

    public Task<AuthenticationStatus> ConfirmEmailAsync(string identifier, string code)
    {
        var id = IdentifierRules.Normalize(identifier);
        if (!id.Contains('@') || !IdentifierRules.IsValid(id) || code is null || code.Trim().Length != 6)
            return Task.FromResult(AuthenticationStatus.InvalidCode);

        return verification.ConfirmAsync(id, code.Trim());
    }
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