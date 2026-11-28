using System.Data;
using System.Linq.Expressions;
using System.Security.Cryptography;
using ASPHotelM2631.Application.Bookings;
using ASPHotelM2631.Domain.Entities;
using ASPHotelM2631.Infrastructure.Persistence.Rooms;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence.Bookings;

public sealed class BookingStore(AppDbContext db) : IBookingStore
{
    private const int HoldMinutes = 15;
    private const int MaxActiveHolds = 3;
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // bỏ I, O, 0, 1 cho dễ đọc

    private sealed record Row(
        string Code, int RoomTypeId, string RoomTypeName, DateOnly CheckInDate, DateOnly CheckOutDate,
        int GuestCount, decimal NightlyRate, decimal TotalAmount, BookingStatus Status,
        DateTime? HoldExpiresAt, DateTime CreatedAt);

    private static readonly Expression<Func<Booking, Row>> ToRow = b => new Row(
        b.Code, b.RoomTypeId, b.RoomType.Name, b.CheckInDate, b.CheckOutDate,
        b.GuestCount, b.NightlyRate, b.TotalAmount, b.Status, b.HoldExpiresAt, b.CreatedAt);

    // Held mà đã quá hạn thì hiển thị là Expired
    private static BookingDto Map(Row r, DateTime now) => new(
        r.Code, r.RoomTypeId, r.RoomTypeName, r.CheckInDate, r.CheckOutDate,
        r.CheckOutDate.DayNumber - r.CheckInDate.DayNumber, r.GuestCount, r.NightlyRate, r.TotalAmount,
        r.Status == BookingStatus.Held && r.HoldExpiresAt <= now ? "Expired" : r.Status.ToString(),
        r.HoldExpiresAt, r.CreatedAt);

    private Expression<Func<Booking, bool>> OwnedBy(string userId) =>
        b => db.Guests.Any(g => g.GuestId == b.GuestId && g.UserId == userId);

    // ---------- Tạo booking (giữ chỗ) ----------
    public async Task<BookingOutcome> CreateHoldAsync(string userId, NewBooking n, DateTime now)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                // 1. Khóa dòng loại phòng: các lượt đặt cùng loại xếp hàng ở đây, khác loại không chặn nhau.
                //    Đổi tên bảng/cột nếu mapping của bạn khác (RoomTypes, RoomTypeId, IsActive).
                var locked = await db.Database
                    .SqlQuery<int>($"SELECT RoomTypeId AS Value FROM RoomTypes WITH (UPDLOCK, ROWLOCK) WHERE RoomTypeId = {n.RoomTypeId} AND IsActive = 1")
                    .ToListAsync();
                if (locked.Count == 0)
                    return new(BookingResultCode.NotFound, Error: "Không tìm thấy loại phòng.");

                var type = await db.RoomTypes.AsNoTracking()
                    .Where(t => t.Id == n.RoomTypeId)
                    .Select(t => new { t.Name, t.BasePrice, t.MaxGuests })
                    .FirstAsync();
                if (n.Guests > type.MaxGuests)
                    return new(BookingResultCode.TooManyGuests, Error: $"Loại phòng này tối đa {type.MaxGuests} khách.");

                var guest = await EnsureGuestAsync(userId, now);

                var activeHolds = await db.Bookings.CountAsync(b =>
                    b.GuestId == guest.GuestId && b.Status == BookingStatus.Held && b.HoldExpiresAt > now);
                if (activeHolds >= MaxActiveHolds)
                    return new(BookingResultCode.TooManyHolds,
                        Error: $"Bạn đang giữ chỗ {MaxActiveHolds} đặt phòng chưa thanh toán. Hãy thanh toán hoặc hủy bớt.");

                // 2. Tính lại công suất từng đêm NGAY TRONG giao dịch đã khóa
                var left = await RoomAvailability.CountAsync(db, n.RoomTypeId, n.CheckIn, n.CheckOut);
                if (left <= 0)
                    return new(BookingResultCode.SoldOut, Error: "Loại phòng này đã hết trong khoảng ngày bạn chọn.");

                // 3. Ghi booking
                db.Bookings.Add(new Booking
                {
                    Code = NewCode(),
                    GuestId = guest.GuestId,
                    RoomTypeId = n.RoomTypeId,
                    CheckInDate = n.CheckIn,
                    CheckOutDate = n.CheckOut,
                    GuestCount = n.Guests,
                    NightlyRate = type.BasePrice,                       // chụp giá lúc đặt
                    TotalAmount = type.BasePrice * n.Nights,
                    Status = BookingStatus.Held,
                    Source = "Online",
                    HoldExpiresAt = now.AddMinutes(HoldMinutes),
                    CreatedAt = now
                });
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                var created = await db.Bookings.AsNoTracking()
                    .OrderByDescending(b => b.Id)
                    .Where(b => b.GuestId == guest.GuestId)
                    .Select(ToRow).FirstAsync();
                return new(BookingResultCode.Ok, Map(created, now));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Trùng mã đặt phòng hoặc hồ sơ khách (hiếm): thử lại
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                // Bế tắc: thử lại
            }
        }
        return new(BookingResultCode.Busy, Error: "Hệ thống đang bận, vui lòng thử lại sau ít giây.");
    }

    private async Task<Guest> EnsureGuestAsync(string userId, DateTime now)
    {
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.UserId == userId);
        if (guest is not null) return guest;

        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.FullName, u.Email, u.PhoneNumber }).FirstAsync();

        guest = new Guest
        {
            UserId = userId,
            FullName = user.FullName,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? "",
            CreatedAt = now
        };
        db.Guests.Add(guest);
        await db.SaveChangesAsync();   // cần GuestId ngay
        return guest;
    }

    private static string NewCode()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return "BK" + new string(chars);
    }

    // ---------- Xem ----------
    public async Task<IReadOnlyList<BookingDto>> ListAsync(string userId, int page, int pageSize, DateTime now)
    {
        var rows = await db.Bookings.AsNoTracking().Where(OwnedBy(userId))
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToRow).ToListAsync();
        return rows.Select(r => Map(r, now)).ToList();
    }

    public async Task<BookingDto?> GetAsync(string userId, string code, DateTime now)
    {
        var row = await db.Bookings.AsNoTracking().Where(OwnedBy(userId)).Where(b => b.Code == code)
            .Select(ToRow).FirstOrDefaultAsync();
        return row is null ? null : Map(row, now);
    }

    // ---------- Hủy: một câu UPDATE có điều kiện, an toàn khi bấm hai lần cùng lúc ----------
    public async Task<BookingOutcome> CancelAsync(string userId, string code, DateOnly today, DateTime now)
    {
        var changed = await db.Bookings
            .Where(OwnedBy(userId))
            .Where(b => b.Code == code
                     && (b.Status == BookingStatus.Confirmed || (b.Status == BookingStatus.Held && b.HoldExpiresAt > now))
                     && b.CheckInDate > today)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Cancelled)
                .SetProperty(b => b.CancelReason, "Khách hủy"));

        if (changed > 0) return new(BookingResultCode.Ok);

        var exists = await db.Bookings.AnyAsync(b => b.Code == code && db.Guests.Any(g => g.GuestId == b.GuestId && g.UserId == userId));
        return exists
            ? new(BookingResultCode.NotAllowed, Error: "Không thể hủy: đặt phòng đã hủy, đã hết hạn giữ chỗ, đang ở hoặc đã đến ngày nhận phòng.")
            : new(BookingResultCode.NotFound, Error: "Không tìm thấy đặt phòng.");
    }

    // ---------- Xác nhận (tạm thời, thay bằng callback cổng thanh toán) ----------
    public async Task<BookingOutcome> ConfirmAsync(string userId, string code, DateTime now)
    {
        var changed = await db.Bookings
            .Where(OwnedBy(userId))
            .Where(b => b.Code == code && b.Status == BookingStatus.Held && b.HoldExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Confirmed)
                .SetProperty(b => b.HoldExpiresAt, (DateTime?)null));

        if (changed > 0) return new(BookingResultCode.Ok, await GetAsync(userId, code, now));

        var exists = await db.Bookings.AnyAsync(b => b.Code == code && db.Guests.Any(g => g.GuestId == b.GuestId && g.UserId == userId));
        return exists
            ? new(BookingResultCode.NotAllowed, Error: "Đặt phòng đã hết hạn giữ chỗ hoặc không ở trạng thái chờ thanh toán.")
            : new(BookingResultCode.NotFound, Error: "Không tìm thấy đặt phòng.");
    }
}
