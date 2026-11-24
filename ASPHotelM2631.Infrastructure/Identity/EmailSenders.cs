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
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config["Smtp:FromName"] ?? "Khách sạn", config["Smtp:From"]!));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;   // tránh lỗi khi máy không liên lạc được máy chủ thu hồi chứng chỉ
        await client.ConnectAsync(config["Smtp:Host"], int.Parse(config["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(config["Smtp:User"], config["Smtp:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
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