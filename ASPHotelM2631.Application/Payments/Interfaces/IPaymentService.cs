namespace ASPHotelM2631.Application.Payments;

public interface IPaymentService
{
    Task<StartOutcome> StartAsync(string userId, string bookingCode);
    Task<IReadOnlyList<PaymentDto>?> ListAsync(string userId, string bookingCode);
    Task<PaymentResultDto?> HandleReturnAsync(string sessionId);
}
