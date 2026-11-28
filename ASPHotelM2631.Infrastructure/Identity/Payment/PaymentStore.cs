using System.Data;
using ASPHotelM2631.Application.Payments;
using ASPHotelM2631.Domain.Entities;
using ASPHotelM2631.Infrastructure.Persistence.Rooms;
using Microsoft.EntityFrameworkCore;

namespace ASPHotelM2631.Infrastructure.Persistence.Payments;

public sealed class PaymentStore(AppDbContext db) : IPaymentStore
{
    public async Task<(StartCode Code, PendingPayment? Pending, string? Error)> CreatePendingAsync(
        string userId, string bookingCode, DateTime nowUtc)
    {
        var b = await db.Bookings.AsNoTracking()
            .Where(x => x.Code == bookingCode && db.Guests.Any(g => g.GuestId == x.GuestId && g.UserId == userId))
            .Select(x => new { x.Id, x.Status, x.HoldExpiresAt, x.TotalAmount })
            .FirstOrDefaultAsync();
        if (b is null) return (StartCode.NotFound, null, "Không tìm thấy đặt phòng.");

        if (b.Status != BookingStatus.Held || b.HoldExpiresAt is null || b.HoldExpiresAt <= nowUtc.AddMinutes(1))
            return (StartCode.NotPayable, null,
                "Đặt phòng không ở trạng thái chờ thanh toán hoặc sắp hết hạn giữ chỗ.");

        var payment = new Payment
        {
            BookingId = b.Id,
            Amount = b.TotalAmount,
            Method = "Card",
            TxnRef = $"{bookingCode}P{Guid.NewGuid():N}",
            Status = "Pending",
            CreatedAt = nowUtc
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        return (StartCode.Ok,
            new PendingPayment(bookingCode, payment.TxnRef, payment.Amount, b.HoldExpiresAt.Value), null);
    }

    public async Task<ApplyCode> ApplyAsync(GatewayResult r, DateTime now)
    {
        var pay = await db.Payments.AsNoTracking()
            .Where(p => p.TxnRef == r.TxnRef)
            .Select(p => new { p.PaymentId, p.BookingId, p.Amount, p.Status })
            .FirstOrDefaultAsync();
        if (pay is null) return ApplyCode.NotFound;
        if (pay.Amount != r.AmountVnd) return ApplyCode.InvalidAmount;
        if (pay.Status != "Pending") return ApplyCode.AlreadyProcessed;

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        // UPDATE có điều kiện: hai request cùng lúc thì chỉ một bên xử lý
        var claimed = await db.Payments
            .Where(p => p.PaymentId == pay.PaymentId && p.Status == "Pending")
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, r.Success ? "Succeeded" : "Failed")
                .SetProperty(p => p.ResponseCode, r.ResponseCode)
                .SetProperty(p => p.GatewayRef, r.TransactionNo)
                .SetProperty(p => p.PaidAt, r.Success ? now : (DateTime?)null));
        if (claimed == 0) return ApplyCode.AlreadyProcessed;

        if (r.Success)
        {
            var b = await db.Bookings.AsNoTracking().Where(x => x.Id == pay.BookingId)
                .Select(x => new { x.RoomTypeId, x.Status, x.HoldExpiresAt, x.CheckInDate, x.CheckOutDate })
                .FirstAsync();

            // Khóa loại phòng để không chen với lượt đặt khác
            await db.Database.SqlQuery<int>(
                $"SELECT RoomTypeId AS Value FROM RoomTypes WITH (UPDLOCK, ROWLOCK) WHERE RoomTypeId = {b.RoomTypeId}")
                .ToListAsync();

            var canConfirm = b.Status == BookingStatus.Held
                && (b.HoldExpiresAt > now
                    || await RoomAvailability.CountAsync(db, b.RoomTypeId, b.CheckInDate, b.CheckOutDate) > 0);

            if (canConfirm)
                await db.Bookings.Where(x => x.Id == pay.BookingId && x.Status == BookingStatus.Held)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, BookingStatus.Confirmed)
                        .SetProperty(x => x.HoldExpiresAt, (DateTime?)null));
            else
                await db.Payments.Where(p => p.PaymentId == pay.PaymentId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "PaidNeedsRefund"));
        }

        await tx.CommitAsync();
        return ApplyCode.Handled;
    }

    public async Task<PaymentResultDto?> GetResultAsync(string txnRef)
    {
        var result = await db.Payments.AsNoTracking().Where(p => p.TxnRef == txnRef)
            .Select(p => new { p.Booking.Code, PaymentStatus = p.Status, p.Booking.Status })
            .FirstOrDefaultAsync();

        return result is null
            ? null
            : new PaymentResultDto(result.Code, result.PaymentStatus, result.Status.ToString());
    }

    public async Task<IReadOnlyList<PaymentDto>?> ListAsync(string userId, string bookingCode)
    {
        var owned = await db.Bookings.AnyAsync(b =>
            b.Code == bookingCode && db.Guests.Any(g => g.GuestId == b.GuestId && g.UserId == userId));
        if (!owned) return null;

        return await db.Payments.AsNoTracking()
            .Where(p => p.Booking.Code == bookingCode)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(p.TxnRef, p.Method, p.Status, p.Amount, p.CreatedAt, p.PaidAt))
            .ToListAsync();
    }
}
