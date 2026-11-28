namespace ASPHotelM2631.Application.Payments;

public interface IPaymentStore
{
    Task<(StartCode Code, PendingPayment? Pending, string? Error)> CreatePendingAsync(
        string userId,
        string bookingCode,
        DateTime nowUtc);

    Task<ApplyCode> ApplyAsync(GatewayResult result, DateTime nowUtc);

    Task<PaymentResultDto?> GetResultAsync(string txnRef);

    Task<IReadOnlyList<PaymentDto>?> ListAsync(string userId, string bookingCode);
}
