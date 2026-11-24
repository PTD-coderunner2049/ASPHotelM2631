namespace ASPHotelM2631.Application.Authentication;

public interface IEmailVerification
{
    /// <summary>Gửi hoặc gửi lại mã. Im lặng (trả Succeeded) nếu email không tồn tại hoặc đã xác thực.</summary>
    Task<AuthenticationStatus> SendCodeAsync(string identifier);

    Task<AuthenticationStatus> ConfirmAsync(string identifier, string code);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}