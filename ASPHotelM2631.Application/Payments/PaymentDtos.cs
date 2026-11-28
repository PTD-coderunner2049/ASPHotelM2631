namespace ASPHotelM2631.Application.Payments;

public sealed record GatewayResult(
    string TxnRef,
    decimal AmountVnd,
    bool Success,
    string ResponseCode,
    string? TransactionNo);

public sealed record CheckoutInfo(string Url, DateTime ExpiresAtUtc);

public sealed record PendingPayment(
    string BookingCode,
    string TxnRef,
    decimal Amount,
    DateTime ExpiresAtUtc);

public sealed record PaymentLinkDto(
    string PaymentUrl,
    string TxnRef,
    DateTime ExpiresAtUtc);

public sealed record PaymentDto(
    string TxnRef,
    string Method,
    string Status,
    decimal Amount,
    DateTime CreatedAt,
    DateTime? PaidAt);

public sealed record PaymentResultDto(
    string BookingCode,
    string PaymentStatus,
    string BookingStatus);
