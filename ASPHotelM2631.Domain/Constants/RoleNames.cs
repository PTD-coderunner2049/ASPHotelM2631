namespace ASPHotelM2631.Domain.Constants;

public static class RoleNames
{
    public const string Customer = "KhachHang";
    public const string Receptionist = "LeTan";
    public const string Housekeeping = "BuongPhong";
    public const string Accountant = "KeToan";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly string[] All =
        [Customer, Receptionist, Housekeeping, Accountant, Manager, Admin];
}