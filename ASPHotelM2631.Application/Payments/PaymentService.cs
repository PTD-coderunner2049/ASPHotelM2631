namespace ASPHotelM2631.Application.Payments;

public sealed class PaymentService(IPaymentStore store, IPaymentGateway gateway) : IPaymentService
{
    public async Task<StartOutcome> StartAsync(string userId, string bookingCode)
    {
        var nowUtc = DateTime.UtcNow;
        var (code, pending, error) = await store.CreatePendingAsync(userId, bookingCode, nowUtc);
        if (code != StartCode.Ok || pending is null)
            return new StartOutcome(code, Error: error);

        var checkout = await gateway.CreateCheckoutAsync(
            pending.TxnRef,
            pending.Amount,
            $"Booking {pending.BookingCode}",
            nowUtc);

        return new StartOutcome(
            StartCode.Ok,
            new PaymentLinkDto(checkout.Url, pending.TxnRef, checkout.ExpiresAtUtc));
    }

    public Task<IReadOnlyList<PaymentDto>?> ListAsync(string userId, string bookingCode) =>
        store.ListAsync(userId, bookingCode);

    public async Task<PaymentResultDto?> HandleReturnAsync(string sessionId)
    {
        var result = await gateway.GetResultAsync(sessionId);
        if (result is null) return null;

        var applyCode = await store.ApplyAsync(result, DateTime.UtcNow);
        if (applyCode is not (ApplyCode.Handled or ApplyCode.AlreadyProcessed)) return null;

        return await store.GetResultAsync(result.TxnRef);
    }
}
