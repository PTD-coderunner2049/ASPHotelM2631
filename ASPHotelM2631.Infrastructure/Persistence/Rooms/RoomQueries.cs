using System.Linq.Expressions;
using ASPHotelM2631.Application.Rooms;
using ASPHotelM2631.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence.Rooms;

public sealed class RoomQueries(AppDbContext db) : IRoomQueries
{
    private static readonly Expression<Func<Room, RoomDto>> ToDto = r => new RoomDto(
        r.Id, r.RoomNumber, r.Floor,
        new RoomTypeDto(r.RoomType.Id, r.RoomType.Name, r.RoomType.Description,
                        r.RoomType.Amenities, r.RoomType.ImageUrl, r.RoomType.MaxGuests,
                        r.RoomType.BasePrice, r.RoomType.IsActive),
        null);

    private IQueryable<Room> PublicRooms() =>
        db.Rooms.AsNoTracking()
            .Where(r => r.IsActive && r.RoomType.IsActive && r.Status != RoomStatus.Maintenance);

    public async Task<PagedResult<RoomDto>> SearchAsync(RoomSearchRequest c)
    {
        var rooms = PublicRooms();

        if (c.RoomTypeId is { } typeId) rooms = rooms.Where(r => r.RoomTypeId == typeId);
        if (c.Guests is { } guests) rooms = rooms.Where(r => r.RoomType.MaxGuests >= guests);
        if (c.MinPrice is { } min) rooms = rooms.Where(r => r.RoomType.BasePrice >= min);
        if (c.MaxPrice is { } max) rooms = rooms.Where(r => r.RoomType.BasePrice <= max);

        // Có ngày thì dựa vào Bookings: loại phòng có đặt phòng còn hiệu lực trùng ngày
        if (c.CheckIn is { } checkIn && c.CheckOut is { } checkOut)
        {
            rooms = rooms.Where(r => !db.Bookings.Any(b =>
                b.RoomId == r.Id
                && b.Status != BookingStatus.Cancelled
                && b.Status != BookingStatus.CheckedOut
                && b.CheckInDate < checkOut
                && b.CheckOutDate > checkIn));
        }

        var total = await rooms.CountAsync();

        var items = await rooms
            .OrderBy(r => r.RoomType.BasePrice).ThenBy(r => r.RoomNumber)
            .Skip((c.Page - 1) * c.PageSize).Take(c.PageSize)
            .Select(ToDto)
            .ToListAsync();

        return new PagedResult<RoomDto>(items, c.Page, c.PageSize, total);
    }

    public Task<RoomDto?> GetAsync(int id) =>
        PublicRooms().Where(r => r.Id == id).Select(ToDto).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<RoomTypeDto>> GetRoomTypesAsync() =>
        await db.RoomTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.BasePrice)
            .Select(t => new RoomTypeDto(
                t.Id, t.Name, t.Description, t.Amenities, t.ImageUrl,
                t.MaxGuests, t.BasePrice, t.IsActive))
            .ToListAsync();
}
