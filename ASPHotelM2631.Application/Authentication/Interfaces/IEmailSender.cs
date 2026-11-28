namespace ASPHotelM2631.Application.Authentication;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}
