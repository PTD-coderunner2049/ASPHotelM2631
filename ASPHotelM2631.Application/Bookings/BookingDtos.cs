namespace ASPHotelM2631.Application.Bookings;

public sealed class CreateBookingRequest
{
    public int RoomTypeId { get; set; }
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Guests { get; set; }
}

public sealed record NewBooking(int RoomTypeId, DateOnly CheckIn, DateOnly CheckOut, int Guests, int Nights);

public sealed record BookingDto(
    string Code, int RoomTypeId, string RoomTypeName,
    DateOnly CheckIn, DateOnly CheckOut, int Nights, int Guests,
    decimal NightlyRate, decimal TotalAmount,
    string Status, DateTime? HoldExpiresAt, DateTime CreatedAt);

public enum BookingResultCode { Ok, Invalid, NotFound, SoldOut, TooManyGuests, TooManyHolds, NotAllowed, Busy }

public sealed record BookingOutcome(BookingResultCode Code, BookingDto? Booking = null, string? Error = null);