namespace ASPHotelM2631.Domain.Bookings;

public enum BookingStatus { Pending, Confirmed, CheckedIn, Completed, Cancelled }
public enum BookingChannel { WalkIn, Phone }

public sealed record Stay
{
    public Stay(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
            throw new ArgumentException("Ngày trả phòng phải sau ngày nhận phòng.");
        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    public DateOnly CheckIn { get; }
    public DateOnly CheckOut { get; }
}

public sealed class GuestProfile
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class Booking
{
    public int Id { get; private set; }
    public int GuestId { get; private set; }
    public GuestProfile Guest { get; private set; } = null!;
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;
    public string? CreatedByUserId { get; private set; }
    public DateOnly CheckIn { get; private set; }
    public DateOnly CheckOut { get; private set; }
    public int GuestCount { get; private set; }
    public BookingChannel Channel { get; private set; }
    public BookingStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal? RefundAmount { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public static Booking Create(GuestProfile guest, Room room, Stay stay, int guestCount,
        BookingChannel channel, decimal total, string userId) =>
        new()
        {
            Guest = guest, Room = room, GuestId = guest.Id, RoomId = room.Id,
            CheckIn = stay.CheckIn, CheckOut = stay.CheckOut, GuestCount = guestCount,
            Channel = channel, Total = total, CreatedByUserId = userId, Status = BookingStatus.Pending
        };

    public bool CanAmend => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    public void Amend(GuestProfile guest, Room room, Stay stay, int guestCount, BookingChannel channel, decimal total)
    {
        Guest = guest;
        GuestId = guest.Id;
        Room = room;
        RoomId = room.Id;
        CheckIn = stay.CheckIn;
        CheckOut = stay.CheckOut;
        GuestCount = guestCount;
        Channel = channel;
        Total = total;
    }

    public void Confirm(decimal paidAmount)
    {
        PaidAmount = paidAmount;
        Status = BookingStatus.Confirmed;
    }

    public void Cancel(DateTimeOffset now, DateTimeOffset checkInTime)
    {
        RefundAmount = checkInTime - now >= TimeSpan.FromHours(24) ? PaidAmount : 0m;
        CancelledAt = now;
        Status = BookingStatus.Cancelled;
    }
}
