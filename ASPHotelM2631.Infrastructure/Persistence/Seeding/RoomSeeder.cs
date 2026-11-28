using ASPHotelM2631.Domain.Entities;
using ASPHotelM2631.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASPHotelM2631.Infrastructure.Persistence.Seeding;

public static class RoomSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var standard = await EnsureRoomTypeAsync(db, "Standard", "Phòng tiêu chuẩn", 2, 500_000, "Wi-Fi, điều hòa");
        var deluxe = await EnsureRoomTypeAsync(db, "Deluxe", "Phòng cao cấp", 3, 850_000, "Wi-Fi, điều hòa, minibar");
        var suite = await EnsureRoomTypeAsync(db, "Suite", "Phòng hạng sang", 4, 1_500_000, "Wi-Fi, điều hòa, minibar, phòng khách");

        await EnsureRoomAsync(db, "101", 1, standard);
        await EnsureRoomAsync(db, "102", 1, standard);
        await EnsureRoomAsync(db, "103", 1, standard, RoomStatus.Maintenance);
        await EnsureRoomAsync(db, "201", 2, deluxe);
        await EnsureRoomAsync(db, "202", 2, deluxe);
        await EnsureRoomAsync(db, "301", 3, suite);
        await db.SaveChangesAsync();

        var guest = await db.Guests.OrderBy(item => item.GuestId).FirstOrDefaultAsync();
        if (guest is null)
        {
            guest = new Guest
            {
                FullName = "Khách mẫu",
                Phone = "0900000000",
                Email = "guest@example.test",
                IdNumber = "DEMO-0001"
            };
            db.Guests.Add(guest);
        }

        var room101 = await db.Rooms.SingleAsync(room => room.RoomNumber == "101");
        if (!await db.Bookings.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            db.Bookings.Add(new Booking
            {
                Code = "DEMO-BOOKING-001",
                Guest = guest,
                RoomType = standard,
                Room = room101,
                CheckInDate = today.AddDays(10),
                CheckOutDate = today.AddDays(13),
                GuestCount = 2,
                TotalAmount = standard.BasePrice * 3,
                Status = BookingStatus.Confirmed,
                Source = "Online"
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<RoomType> EnsureRoomTypeAsync(
        AppDbContext db, string name, string description, int maxGuests, decimal basePrice, string amenities)
    {
        var roomType = await db.RoomTypes.SingleOrDefaultAsync(item => item.Name == name);
        if (roomType is not null)
        {
            if (string.IsNullOrWhiteSpace(roomType.Amenities))
                roomType.Amenities = amenities;
            return roomType;
        }

        roomType = new RoomType
        {
            Name = name,
            Description = description,
            Amenities = amenities,
            MaxGuests = maxGuests,
            BasePrice = basePrice
        };
        db.RoomTypes.Add(roomType);
        return roomType;
    }

    private static async Task EnsureRoomAsync(
        AppDbContext db, string roomNumber, int floor, RoomType roomType,
        RoomStatus status = RoomStatus.Available)
    {
        if (await db.Rooms.AnyAsync(item => item.RoomNumber == roomNumber)) return;

        db.Rooms.Add(new Room
        {
            RoomNumber = roomNumber,
            Floor = floor,
            RoomType = roomType,
            Status = status
        });
    }
}
