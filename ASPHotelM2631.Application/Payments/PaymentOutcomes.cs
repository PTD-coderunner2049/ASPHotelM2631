namespace ASPHotelM2631.Application.Payments;

public enum StartCode
{
    Ok,
    NotFound,
    NotPayable
}

public sealed record StartOutcome(
    StartCode Code,
    PaymentLinkDto? Link = null,
    string? Error = null);

public enum ApplyCode
{
    Handled,
    NotFound,
    InvalidAmount,
    AlreadyProcessed
}
