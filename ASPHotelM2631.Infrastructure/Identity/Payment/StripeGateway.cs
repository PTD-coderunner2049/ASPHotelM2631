using System.Net;
using ASPHotelM2631.Application.Payments;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace ASPHotelM2631.Infrastructure.Payments;

public sealed class StripeGateway(IConfiguration config) : IPaymentGateway
{
    private SessionService Sessions() => new(new StripeClient(Required("Stripe:SecretKey")));

    public async Task<CheckoutInfo> CreateCheckoutAsync(
        string txnRef, decimal amountVnd, string description, DateTime nowUtc)
    {
        var expires = nowUtc.AddMinutes(31);   // tối thiểu 30 phút
        var session = await Sessions().CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = txnRef,
            SuccessUrl = Required("Stripe:SuccessUrl"),
            CancelUrl = Required("Stripe:CancelUrl"),
            ExpiresAt = expires,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "vnd",
                        UnitAmount = (long)amountVnd,   // VND không nhân 100
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = description }
                    }
                }
            ]
        });
        return new CheckoutInfo(session.Url, expires);
    }

    public async Task<GatewayResult?> GetResultAsync(string sessionId)
    {
        try
        {
            var s = await Sessions().GetAsync(sessionId);
            var paid = s.PaymentStatus == "paid";
            if (!paid && s.Status != "expired") return null;

            return new GatewayResult(s.ClientReferenceId, s.AmountTotal ?? 0, paid,
                paid ? "paid" : "expired", s.PaymentIntentId);
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private string Required(string key) =>
        config[key] ?? throw new InvalidOperationException($"Thiếu {key}.");
}