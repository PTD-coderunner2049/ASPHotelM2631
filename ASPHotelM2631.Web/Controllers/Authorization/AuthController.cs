using System.Security.Claims;
using ASPHotelM2631.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers.Authorization;

[ApiController]
[Route("api")]
public sealed class AuthController(IAuthenticationService auth) : ControllerBase
{
    [HttpPost("users")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var status = await auth.RegisterAsync(request.FullName, request.Identifier, request.Password);
        return status switch
        {
            AuthenticationStatus.Succeeded => StatusCode(201, new
            {
                message = "Đăng ký thành công.",
                requiresEmailVerification = request.Identifier.Contains('@')
            }),
            AuthenticationStatus.DuplicateAccount => Conflict(new { message = "Email hoặc số điện thoại này đã được đăng ký." }),
            _ => BadRequest(new { message = "Thông tin đăng ký không hợp lệ." })
        };
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await auth.SignInAsync(request.Identifier, request.Password);
        return result.Status switch
        {
            AuthenticationStatus.Succeeded => StatusCode(201, new
            {
                message = "Đăng nhập thành công.",
                tokenType = "Bearer",
                token = result.Token,
                expiresAt = result.ExpiresAt,
                user = new { id = result.UserId, fullName = result.FullName, role = result.Role }
            }),
            AuthenticationStatus.EmailNotConfirmed => StatusCode(403, new
            {
                requiresEmailVerification = true,
                message = "Email chưa được xác thực. Vui lòng nhập mã OTP đã gửi tới email."
            }),
            AuthenticationStatus.LockedOut => StatusCode(423, new { message = "Tài khoản bị khóa tạm thời. Vui lòng thử lại sau 15 phút." }),
            AuthenticationStatus.InvalidInput => BadRequest(new { message = "Thông tin đăng nhập không hợp lệ." }),
            _ => Unauthorized(new { message = "Email/số điện thoại hoặc mật khẩu không đúng." })
        };
    }

    [Authorize]
    [HttpGet("sessions/current")]
    public IActionResult Current() => Ok(new
    {
        id = User.FindFirstValue("sub"),
        fullName = User.FindFirstValue("fullName"),
        userName = User.FindFirstValue("name"),
        role = User.FindFirstValue("role")
    });

    [Authorize]
    [HttpDelete("sessions/current")]
    public async Task<IActionResult> Logout()
    {
        await auth.SignOutAsync();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("email-verifications")]   // gửi hoặc gửi lại mã
    public async Task<IActionResult> SendCode(EmailRequest request)
    {
        var status = await auth.SendVerificationCodeAsync(request.Identifier);
        return status == AuthenticationStatus.TooManyRequests
            ? StatusCode(429, new { message = "Vui lòng đợi 60 giây trước khi yêu cầu mã mới." })
            : Accepted(new { message = "Mã xác thực đã được gửi tới email của bạn. Nếu chưa thấy, hãy kiểm tra thư rác hoặc thử gửi lại sau 60 giây." });
    }

    [AllowAnonymous]
    [HttpPost("email-verifications/confirm")]   // nhập mã
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var status = await auth.ConfirmEmailAsync(request.Identifier, request.Code);
        return status == AuthenticationStatus.Succeeded
            ? Ok(new { message = "Xác thực email thành công. Bạn có thể đăng nhập." })
            : BadRequest(new { message = "Mã không đúng hoặc đã hết hạn." });
    }
}

public sealed record RegisterRequest(string FullName, string Identifier, string Password);
public sealed record LoginRequest(string Identifier, string Password);
public sealed record EmailRequest(string Identifier);
public sealed record ConfirmEmailRequest(string Identifier, string Code);