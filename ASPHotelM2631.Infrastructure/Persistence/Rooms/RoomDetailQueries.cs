using ASPHotelM2631.Application.Rooms;
using ASPHotelM2631.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence;

public sealed class RoomTypeQueries(AppDbContext db) : IRoomTypeQueries
{
    public Task<RoomTypeRecord?> GetActiveAsync(int id) =>
        db.RoomTypes.AsNoTracking()
            .Where(t => t.Id == id && t.IsActive)      // loại phòng ngừng bán thì 404
            .Select(t => new RoomTypeRecord(
                t.Id, t.Name, t.Description, t.Amenities, t.ImageUrl, t.MaxGuests, t.BasePrice))
            .FirstOrDefaultAsync();

    public Task<int> CountAvailableAsync(int roomTypeId, DateOnly checkIn, DateOnly checkOut) =>
        RoomAvailability.CountAsync(db, roomTypeId, checkIn, checkOut);
}

public sealed class RoomStaffQueries(AppDbContext db) : IRoomStaffQueries
{
    public async Task<RoomStaffDetailDto?> GetAsync(int roomId, DateOnly today)
    {
        var room = await db.Rooms.AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new
            {
                r.Id, r.RoomNumber, r.Floor, r.Status,
                TypeId = r.RoomType.Id, TypeName = r.RoomType.Name,
                r.RoomType.MaxGuests, r.RoomType.BasePrice
            })
            .FirstOrDefaultAsync();
        if (room is null) return null;

        // Chỉ lấy mã đặt phòng và ngày, không lấy thông tin khách
        var stays = await db.Bookings.AsNoTracking()
            .Where(b => b.RoomId == roomId
                     && (b.Status == BookingStatus.CheckedIn
                         || (b.Status == BookingStatus.Confirmed && b.CheckInDate >= today)))
            .OrderBy(b => b.CheckInDate)
            .Take(6)
            .Select(b => new
            {
                b.Id, b.Code, b.CheckInDate, b.CheckOutDate, b.Status
            })
            .ToListAsync();

        var stayDtos = stays
            .Select(s => new RoomStayDto(
                s.Id, s.Code, s.CheckInDate, s.CheckOutDate, s.Status.ToString()))
            .ToList();

        return new RoomStaffDetailDto(
            room.Id, room.RoomNumber, room.Floor, room.Status.ToString(),
            new RoomTypeSummaryDto(room.TypeId, room.TypeName, room.MaxGuests, room.BasePrice),
            CurrentStay: stayDtos.FirstOrDefault(s => s.Status == nameof(BookingStatus.CheckedIn)),
            Upcoming: stayDtos.Where(s => s.Status != nameof(BookingStatus.CheckedIn)).ToList());
    }
}