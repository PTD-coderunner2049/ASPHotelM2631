namespace HotelManagement.Application.Authentication;

public enum AuthenticationStatus
{
    Succeeded,
    InvalidInput,
    DuplicateAccount,
    InvalidCredentials,
    LockedOut
}

public sealed record AuthenticationOutcome(AuthenticationStatus Status);
