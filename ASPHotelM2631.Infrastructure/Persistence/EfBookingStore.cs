using System.Data;
using ASPHotelM2631.Application.Bookings;
using ASPHotelM2631.Domain.Bookings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence;

public sealed class EfBookingStore(AppDbContext db) : IBookingStore, IGuestStore
{
    public async Task<IReadOnlyList<Room>> FindAvailableRoomsAsync(Stay stay, int? roomTypeId,
        CancellationToken cancellationToken)
    {
        var conflicts = BlockingBookings(stay);
        var query = db.Rooms.AsNoTracking()
            .Where(room => room.Type.Status == RoomTypeStatus.Active && room.Condition != RoomCondition.Maintenance &&
                !conflicts.Any(booking => booking.RoomId == room.Id));
        if (roomTypeId is { } typeId)
            query = query.Where(room => room.RoomTypeId == typeId);

        return await query.Include(room => room.Type).ThenInclude(type => type.Rates)
            .OrderBy(room => room.Number).ThenBy(room => room.Id).ToListAsync(cancellationToken);
    }

    public Task<Room?> FindRoomAsync(int id, CancellationToken cancellationToken) =>
        db.Rooms.Include(room => room.Type).ThenInclude(type => type.Rates)
            .SingleOrDefaultAsync(room => room.Id == id, cancellationToken);

    public Task<bool> IsRoomBlockedAsync(int roomId, Stay stay, int? exceptBooking,
        CancellationToken cancellationToken)
    {
        var query = BlockingBookings(stay).Where(booking => booking.RoomId == roomId);
        if (exceptBooking is { } id)
            query = query.Where(booking => booking.Id != id);

        return query.AnyAsync(cancellationToken);
    }

    public Task<Booking?> FindBookingAsync(int id, CancellationToken cancellationToken) =>
        db.Bookings.Include(booking => booking.Guest).Include(booking => booking.Room)
            .SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public async Task<Page<BookingDetails>> FindBookingsAsync(BookingSearch search, CancellationToken cancellationToken)
    {
        var query = db.Bookings.AsNoTracking();
        if (search.Status is { } status)
            query = query.Where(booking => booking.Status == status);
        if (search.From is { } from && search.To is { } to)
            query = query.Where(booking => booking.CheckIn < to && from < booking.CheckOut);
        if (!string.IsNullOrEmpty(search.Search))
        {
            var keyword = search.Search;
            var bookingId = int.TryParse(keyword, out var id) ? id : 0;
            query = query.Where(booking => booking.Id == bookingId || booking.Guest.FullName.Contains(keyword) ||
                booking.Guest.Phone.Contains(keyword) || booking.Room.Number.Contains(keyword));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(booking => booking.CheckIn).ThenByDescending(booking => booking.Id)
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(booking => new BookingDetails(booking.Id, booking.GuestId, booking.Guest.FullName,
                booking.RoomId, booking.Room.Number, booking.CheckIn, booking.CheckOut, booking.GuestCount,
                booking.Channel.ToString(), booking.Status.ToString(), booking.Total, booking.PaidAmount,
                booking.RefundAmount, booking.CancelledAt))
            .ToListAsync(cancellationToken);
        return new Page<BookingDetails>(items, total, search.Page, search.PageSize);
    }

    public Task<GuestProfile?> FindGuestAsync(int id, CancellationToken cancellationToken) =>
        db.Guests.SingleOrDefaultAsync(guest => guest.Id == id, cancellationToken);

    public async Task<Page<GuestDetails>> FindGuestsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Guests.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(guest => guest.FullName.Contains(search) || guest.Phone.Contains(search) ||
                (guest.Email != null && guest.Email.Contains(search)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(guest => guest.FullName).ThenBy(guest => guest.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(guest => new GuestDetails(guest.Id, guest.FullName, guest.Phone, guest.Email))
            .ToListAsync(cancellationToken);
        return new Page<GuestDetails>(items, total, page, pageSize);
    }

    public void Add(Booking booking) => db.Bookings.Add(booking);
    public void Add(GuestProfile guest) => db.Guests.Add(guest);
    public async Task SaveAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);

    public async Task<T> SaveBookingAsync<T>(Func<CancellationToken, Task<T>> change, CancellationToken cancellationToken)
    {
        try
        {
            // Keep the overlap check and write in the same serializable transaction.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await change(cancellationToken);
            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error) when (error is DbUpdateConcurrencyException || IsSqlDeadlock(error))
        {
            throw new BookingException(BookingFailure.Conflict, "Dữ liệu đặt phòng vừa thay đổi. Vui lòng tải lại và thử lại.");
        }
    }

    private IQueryable<Booking> BlockingBookings(Stay stay) => db.Bookings.Where(booking =>
        (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed ||
            booking.Status == BookingStatus.CheckedIn) &&
        booking.CheckIn < stay.CheckOut && stay.CheckIn < booking.CheckOut);

    private static bool IsSqlDeadlock(Exception error)
    {
        // EF Core can wrap a SQL Server deadlock in both an execution and an update exception.
        for (Exception? cause = error; cause is not null; cause = cause.InnerException)
            if (cause is SqlException { Number: 1205 })
                return true;

        return false;
    }
}
