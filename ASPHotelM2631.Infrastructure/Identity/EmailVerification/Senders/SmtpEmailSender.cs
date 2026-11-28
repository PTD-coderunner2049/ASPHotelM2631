using ASPHotelM2631.Application.Authentication;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ASPHotelM2631.Infrastructure.Identity.EmailVerification.Senders;

/// <summary>Sends email through the configured SMTP server.</summary>
public sealed class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var from = RequiredSetting("Smtp:From");
        var host = RequiredSetting("Smtp:Host");
        var user = RequiredSetting("Smtp:User");
        var password = RequiredSetting("Smtp:Password");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config["Smtp:FromName"] ?? "Khách sạn", from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(host, int.Parse(config["Smtp:Port"] ?? "587"), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string RequiredSetting(string key) =>
        !string.IsNullOrWhiteSpace(config[key])
            ? config[key]!
            : throw new InvalidOperationException($"Missing required email setting '{key}'.");
}
