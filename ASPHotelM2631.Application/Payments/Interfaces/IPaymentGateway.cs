namespace ASPHotelM2631.Application.Payments;

public interface IPaymentGateway
{
    Task<CheckoutInfo> CreateCheckoutAsync(
        string txnRef,
        decimal amountVnd,
        string description,
        DateTime nowUtc);

    Task<GatewayResult?> GetResultAsync(string sessionId);
}
