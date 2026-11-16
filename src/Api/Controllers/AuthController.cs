using System.Security.Claims;
using HotelManagement.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await authenticationService.RegisterAsync(
            request.FullName,
            request.Identifier,
            request.Password);

        return result switch
        {
            AuthenticationStatus.Succeeded => Ok(new { message = "Đăng ký thành công." }),
            AuthenticationStatus.DuplicateAccount => Conflict(new
            {
                message = "Email hoặc số điện thoại này đã được đăng ký."
            }),
            _ => BadRequest(new { message = "Thông tin đăng ký không hợp lệ." })
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await authenticationService.SignInAsync(request.Identifier, request.Password);

        return result switch
        {
            AuthenticationStatus.Succeeded => Ok(new { message = "Đăng nhập thành công." }),
            AuthenticationStatus.LockedOut => StatusCode(StatusCodes.Status423Locked, new
            {
                message = "Tài khoản bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút."
            }),
            _ => Unauthorized(new { message = "Email/số điện thoại hoặc mật khẩu không đúng." })
        };
    }

    // [Authorize]
    // [HttpGet("me")]
    // public IActionResult Me() => Ok(new
    // {
    //     id = User.FindFirstValue(ClaimTypes.NameIdentifier),
    //     name = User.FindFirstValue("display_name"),
    //     email = User.FindFirstValue(ClaimTypes.Email)
    // });

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authenticationService.SignOutAsync();
        return Ok(new { message = "Đã đăng xuất." });
    }

    public sealed record RegisterRequest(string FullName, string Identifier, string Password);
    public sealed record LoginRequest(string Identifier, string Password);
}
