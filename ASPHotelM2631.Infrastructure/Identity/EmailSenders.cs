using ASPHotelM2631.Application.Authentication;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ASPHotelM2631.Infrastructure.Identity;

/// <summary>Gửi mail thật qua SMTP.</summary>
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
        client.CheckCertificateRevocation = false;   // tránh lỗi khi máy không liên lạc được máy chủ thu hồi chứng chỉ
        await client.ConnectAsync(host, int.Parse(config["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string RequiredSetting(string key) =>
        !string.IsNullOrWhiteSpace(config[key])
            ? config[key]!
            : throw new InvalidOperationException($"Missing required email setting '{key}'.");
}

/// <summary>Chế độ thử: không gửi mail, in nội dung ra terminal để đọc mã.</summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        logger.LogWarning("[EMAIL GIẢ LẬP] Tới: {To} | {Subject} | {Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
