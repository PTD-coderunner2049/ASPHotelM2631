using System.Security.Claims;
using ASPHotelM2631.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers;

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
            AuthenticationStatus.Succeeded => StatusCode(201, new { message = "Đăng ký thành công." }),
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
}

public sealed record RegisterRequest(string FullName, string Identifier, string Password);
public sealed record LoginRequest(string Identifier, string Password);