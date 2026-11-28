using System.Security.Claims;
using ASPHotelM2631.Application.Payments;
using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers;

[ApiController]
[Route("api")]
public sealed class PaymentsController(IPaymentService payments, IConfiguration config) : ControllerBase
{
    private string UserId => User.FindFirstValue("sub")!;

    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("bookings/{code}/payments")]
    public async Task<IActionResult> Start(string code)
    {
        var outcome = await payments.StartAsync(UserId, code);
        return outcome.Code switch
        {
            StartCode.Ok => Ok(outcome.Link),
            StartCode.NotFound => NotFound(new { message = outcome.Error }),
            _ => Conflict(new { message = outcome.Error })
        };
    }

    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("bookings/{code}/payments")]
    public async Task<IActionResult> List(string code)
    {
        var list = await payments.ListAsync(UserId, code);
        return list is null
            ? NotFound(new { message = "Không tìm thấy đặt phòng." })
            : Ok(list);
    }

    [AllowAnonymous]
    [HttpGet("payments/stripe/return")]
    public async Task<IActionResult> Return([FromQuery(Name = "session_id")] string sessionId)
    {
        var result = await payments.HandleReturnAsync(sessionId);
        if (result is null)
            return BadRequest(new { message = "Không xác nhận được thanh toán." });

        var frontend = config["Stripe:FrontendResultUrl"];
        return string.IsNullOrWhiteSpace(frontend)
            ? Ok(result)
            : Redirect($"{frontend}?code={Uri.EscapeDataString(result.BookingCode)}&status={Uri.EscapeDataString(result.PaymentStatus)}");
    }

    [AllowAnonymous]
    [HttpGet("payments/stripe/cancel")]
    public IActionResult Cancel() => Ok(new { message = "Bạn đã hủy thanh toán." });
}
