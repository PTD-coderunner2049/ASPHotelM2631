namespace ASPHotelM2631.Domain.Entities;

public class Guest
{
    public int GuestId { get; set; }
    public string? UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IdNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Booking> Bookings { get; set; } = [];
}
