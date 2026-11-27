using ASPHotelM2631.Application.Rooms;

namespace ASPHotelM2631.Application.Bookings;

public sealed class BookingService(IBookingStore store) : IBookingService
{
    public Task<BookingOutcome> CreateAsync(string userId, CreateBookingRequest r)
    {
        if (r.RoomTypeId <= 0 || r.CheckIn is null || r.CheckOut is null)
            return Task.FromResult(Invalid("Cần chọn loại phòng, ngày nhận phòng và ngày trả phòng."));

        var error = StayRules.Validate(r.CheckIn, r.CheckOut, r.Guests, out var nights);
        if (error is not null) return Task.FromResult(Invalid(error));

        return store.CreateHoldAsync(userId,
            new NewBooking(r.RoomTypeId, r.CheckIn.Value, r.CheckOut.Value, r.Guests, nights),
            DateTime.UtcNow);
    }

    public Task<IReadOnlyList<BookingDto>> ListAsync(string userId, int page, int pageSize) =>
        store.ListAsync(userId, Math.Max(page, 1), Math.Clamp(pageSize, 1, 50), DateTime.UtcNow);

    public Task<BookingDto?> GetAsync(string userId, string code) =>
        store.GetAsync(userId, Normalize(code), DateTime.UtcNow);

    public Task<BookingOutcome> CancelAsync(string userId, string code) =>
        store.CancelAsync(userId, Normalize(code), StayRules.Today(), DateTime.UtcNow);

    public Task<BookingOutcome> ConfirmAsync(string userId, string code) =>
        store.ConfirmAsync(userId, Normalize(code), DateTime.UtcNow);

    private static string Normalize(string code) => (code ?? "").Trim().ToUpperInvariant();
    private static BookingOutcome Invalid(string message) => new(BookingResultCode.Invalid, Error: message);
}
