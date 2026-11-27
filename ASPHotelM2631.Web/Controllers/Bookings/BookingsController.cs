using System.Security.Claims;
using ASPHotelM2631.Application.Bookings;
using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Customer)]
[Route("api/bookings")]
public sealed class BookingsController(IBookingService bookings, IWebHostEnvironment env) : ControllerBase
{
    private string UserId => User.FindFirstValue("sub")!;

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        var o = await bookings.CreateAsync(UserId, request);
        return o.Code switch
        {
            BookingResultCode.Ok => CreatedAtAction(nameof(Get), new { code = o.Booking!.Code }, o.Booking),
            BookingResultCode.Invalid or BookingResultCode.TooManyGuests => BadRequest(new { message = o.Error }),
            BookingResultCode.NotFound => NotFound(new { message = o.Error }),
            BookingResultCode.SoldOut or BookingResultCode.TooManyHolds => Conflict(new { message = o.Error }),
            _ => StatusCode(503, new { message = o.Error })
        };
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await bookings.ListAsync(UserId, page, pageSize));

    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code)
    {
        var b = await bookings.GetAsync(UserId, code);
        return b is null ? NotFound(new { message = "Không tìm thấy đặt phòng." }) : Ok(b);
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Cancel(string code)
    {
        var o = await bookings.CancelAsync(UserId, code);
        return o.Code switch
        {
            BookingResultCode.Ok => NoContent(),
            BookingResultCode.NotFound => NotFound(new { message = o.Error }),
            _ => Conflict(new { message = o.Error })
        };
    }

    // CHỈ ĐỂ TEST: giả lập thanh toán thành công. Xóa action này khi làm cổng thanh toán thật.
    [HttpPost("{code}/simulated-payment")]
    public async Task<IActionResult> SimulatedPayment(string code)
    {
        if (!env.IsDevelopment()) return NotFound();

        var o = await bookings.ConfirmAsync(UserId, code);
        return o.Code switch
        {
            BookingResultCode.Ok => Ok(o.Booking),
            BookingResultCode.NotFound => NotFound(new { message = o.Error }),
            _ => Conflict(new { message = o.Error })
        };
    }
}