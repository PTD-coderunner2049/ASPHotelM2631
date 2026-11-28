using ASPHotelM2631.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence;

internal static class RoomAvailability
{
    // Booking còn chiếm chỗ: đã xác nhận, đang ở, hoặc đang giữ chỗ mà chưa hết hạn
    public static IQueryable<Booking> Occupying(this IQueryable<Booking> bookings, DateTime nowUtc) =>
        bookings.Where(b => b.Status == BookingStatus.Confirmed
                         || b.Status == BookingStatus.CheckedIn
                         || (b.Status == BookingStatus.Held && b.HoldExpiresAt > nowUtc)
                         || (b.Status == BookingStatus.Pending && b.HoldExpiresAt > nowUtc));

    public static async Task<int> CountAsync(AppDbContext db, int roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        // Công suất = số phòng đang hoạt động của loại này (nếu Room có cột IsActive, thêm điều kiện)
        var capacity = await db.Rooms.CountAsync(r =>
            r.IsActive && r.RoomTypeId == roomTypeId && r.Status != RoomStatus.Maintenance);
        if (capacity == 0) return 0;

        var bookings = await db.Bookings.AsNoTracking()
            .Occupying(DateTime.UtcNow)
            .Where(b => b.RoomTypeId == roomTypeId
                     && b.CheckInDate < checkOut && b.CheckOutDate > checkIn)
            .Select(b => new { b.CheckInDate, b.CheckOutDate })
            .ToListAsync();

        // Tính từng đêm: lấy đêm đông nhất. Đếm gộp cả kỳ sẽ báo thiếu phòng sai.
        var minLeft = capacity;
        for (var night = checkIn; night < checkOut; night = night.AddDays(1))
        {
            var booked = bookings.Count(b => b.CheckInDate <= night && b.CheckOutDate > night);
            minLeft = Math.Min(minLeft, capacity - booked);
        }
        return Math.Max(minLeft, 0);
    }
}
