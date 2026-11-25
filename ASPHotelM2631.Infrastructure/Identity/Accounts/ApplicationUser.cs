using Microsoft.AspNetCore.Identity;

namespace ASPHotelM2631.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
}
