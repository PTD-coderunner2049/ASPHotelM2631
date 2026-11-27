using ASPHotelM2631.Domain.Bookings;

namespace ASPHotelM2631.Application.Bookings;

public enum BookingFailure { Invalid, NotFound, Conflict }

public sealed class BookingException(BookingFailure failure, string message) : Exception(message)
{
    public BookingFailure Failure { get; } = failure;
}

public sealed record AvailableRoom(int Id, string Number, int Floor, int RoomTypeId,
    string RoomType, decimal Total);
public sealed record BookingInput(int GuestId, int RoomId, DateOnly CheckIn, DateOnly CheckOut,
    int GuestCount, string Channel);
public sealed record BookingDetails(int Id, int GuestId, string GuestName, int RoomId, string RoomNumber,
    DateOnly CheckIn, DateOnly CheckOut, int GuestCount, string Channel, string Status, decimal Total,
    decimal PaidAmount, decimal? RefundAmount, DateTimeOffset? CancelledAt);
public sealed record BookingSearch(string? Search, BookingStatus? Status, DateOnly? From,
    DateOnly? To, int Page = 1, int PageSize = 20);
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int Number, int Size);
public sealed record GuestInput(string FullName, string Phone, string? Email);
public sealed record GuestDetails(int Id, string FullName, string Phone, string? Email);

public interface IGuestStore
{
    Task<GuestProfile?> FindGuestAsync(int id, CancellationToken cancellationToken);
    Task<Page<GuestDetails>> FindGuestsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    void Add(GuestProfile guest);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IBookingStore
{
    Task<IReadOnlyList<Room>> FindAvailableRoomsAsync(Stay stay, int? roomTypeId, CancellationToken cancellationToken);
    Task<Room?> FindRoomAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsRoomBlockedAsync(int roomId, Stay stay, int? exceptBooking, CancellationToken cancellationToken);
    Task<Booking?> FindBookingAsync(int id, CancellationToken cancellationToken);
    Task<Page<BookingDetails>> FindBookingsAsync(BookingSearch search, CancellationToken cancellationToken);
    void Add(Booking booking);
    Task<T> SaveBookingAsync<T>(Func<CancellationToken, Task<T>> change, CancellationToken cancellationToken);
}
