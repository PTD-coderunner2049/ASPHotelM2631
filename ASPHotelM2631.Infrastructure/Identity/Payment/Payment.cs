namespace ASPHotelM2631.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public string Type { get; set; } = "Payment"; // Deposit, Payment, Refund
    public string Method { get; set; } = "Card"; // Cash, Card, Transfer, VNPay, Momo
    public string Status { get; set; } = "Pending"; // Pending, Succeeded, Failed, PaidNeedsRefund
    public decimal Amount { get; set; } // VND
    public string TxnRef { get; set; } = "";
    public string? GatewayRef { get; set; }
    public string? ResponseCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
