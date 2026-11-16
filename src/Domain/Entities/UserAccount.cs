namespace HotelManagement.Domain.Entities;

public sealed class UserAccount
{
    public UserAccount(Guid id, string fullName, string identifier, string? email, string? phoneNumber)
    {
        Id = id;
        FullName = fullName;
        Identifier = identifier;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    public Guid Id { get; }
    public string FullName { get; }
    public string Identifier { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }
}
