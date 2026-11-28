namespace ASPHotelM2631.Application.Bookings;

public interface IBookingService
{
    Task<BookingOutcome> CreateAsync(string userId, CreateBookingRequest request);
    Task<IReadOnlyList<BookingDto>> ListAsync(string userId, int page, int pageSize);
    Task<BookingDto?> GetAsync(string userId, string code);
    Task<BookingOutcome> CancelAsync(string userId, string code);
    Task<BookingOutcome> ConfirmAsync(string userId, string code);
}
