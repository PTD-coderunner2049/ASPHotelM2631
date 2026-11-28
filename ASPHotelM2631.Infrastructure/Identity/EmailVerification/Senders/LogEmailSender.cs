using ASPHotelM2631.Application.Authentication;
using Microsoft.Extensions.Logging;

namespace ASPHotelM2631.Infrastructure.Identity.EmailVerification.Senders;

/// <summary>Development sender that writes email details to the application log.</summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        logger.LogWarning("[EMAIL GIẢ LẬP] Tới: {To} | {Subject} | {Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
