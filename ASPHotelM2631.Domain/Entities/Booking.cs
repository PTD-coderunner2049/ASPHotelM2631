namespace ASPHotelM2631.Domain.Entities;

public enum BookingStatus { Pending, Held, Confirmed, CheckedIn, CheckedOut, Cancelled }

public class Booking
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal NightlyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string Source { get; set; } = "Online";
    public string? CancelReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime? HoldExpiresAt { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
