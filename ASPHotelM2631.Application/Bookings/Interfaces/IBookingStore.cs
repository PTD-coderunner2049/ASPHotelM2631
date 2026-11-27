namespace ASPHotelM2631.Application.Bookings;

public interface IBookingStore
{
    Task<BookingOutcome> CreateHoldAsync(string userId, NewBooking booking, DateTime nowUtc);
    Task<IReadOnlyList<BookingDto>> ListAsync(string userId, int page, int pageSize, DateTime nowUtc);
    Task<BookingDto?> GetAsync(string userId, string code, DateTime nowUtc);
    Task<BookingOutcome> CancelAsync(string userId, string code, DateOnly today, DateTime nowUtc);
    Task<BookingOutcome> ConfirmAsync(string userId, string code, DateTime nowUtc);   // thay bằng callback thanh toán sau
}