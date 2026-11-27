using System.Security.Claims;
using ASPHotelM2631.Application.Bookings;
using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ASPHotelM2631.Web.Controllers;

[ApiController]
[Route("api/reception")]
[Authorize(Roles = RoleNames.Receptionist)]
[BookingErrors]
public sealed class ReceptionController(BookingService bookings, GuestService guests) : ControllerBase
{
    [HttpGet("rooms/availability")]
    public Task<IReadOnlyList<AvailableRoom>> Availability([FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut,
        [FromQuery] int? roomTypeId, CancellationToken cancellationToken) =>
        bookings.FindAvailableAsync(checkIn, checkOut, roomTypeId, cancellationToken);

    [HttpGet("guests")]
    public Task<Page<GuestDetails>> ListGuests([FromQuery] string? search, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        guests.SearchAsync(search, page, pageSize, cancellationToken);

    [HttpGet("guests/{id:int}")]
    public Task<GuestDetails> Guest(int id, CancellationToken cancellationToken) =>
        guests.GetAsync(id, cancellationToken);

    [HttpPost("guests")]
    public async Task<ActionResult<GuestDetails>> CreateGuest(GuestInput input, CancellationToken cancellationToken)
    {
        var guest = await guests.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(Guest), new { id = guest.Id }, guest);
    }

    [HttpPut("guests/{id:int}")]
    public Task<GuestDetails> UpdateGuest(int id, GuestInput input, CancellationToken cancellationToken) =>
        guests.UpdateAsync(id, input, cancellationToken);

    [HttpGet("bookings")]
    public Task<Page<BookingDetails>> ListBookings([FromQuery] BookingSearch search, CancellationToken cancellationToken) =>
        bookings.SearchAsync(search, cancellationToken);

    [HttpGet("bookings/{id:int}")]
    public Task<BookingDetails> Booking(int id, CancellationToken cancellationToken) =>
        bookings.GetAsync(id, cancellationToken);

    [HttpPost("bookings")]
    public async Task<ActionResult<BookingDetails>> CreateBooking(BookingInput input, CancellationToken cancellationToken)
    {
        var booking = await bookings.CreateAsync(input, User.FindFirstValue("sub")!, cancellationToken);
        return CreatedAtAction(nameof(Booking), new { id = booking.Id }, booking);
    }

    [HttpPut("bookings/{id:int}")]
    public Task<BookingDetails> UpdateBooking(int id, BookingInput input, CancellationToken cancellationToken) =>
        bookings.AmendAsync(id, input, cancellationToken);

    [HttpPost("bookings/{id:int}/confirm")]
    public Task<BookingDetails> ConfirmBooking(int id, ConfirmReceptionBooking input, CancellationToken cancellationToken) =>
        bookings.ConfirmAsync(id, input.PaidAmount, cancellationToken);

    [HttpPost("bookings/{id:int}/cancel")]
    public Task<BookingDetails> CancelBooking(int id, CancellationToken cancellationToken) =>
        bookings.CancelAsync(id, cancellationToken);
}

public sealed record ConfirmReceptionBooking(decimal PaidAmount);

internal sealed class BookingErrorsAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not BookingException error)
            return;

        var status = error.Failure switch
        {
            BookingFailure.Invalid => StatusCodes.Status400BadRequest,
            BookingFailure.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status409Conflict
        };
        context.Result = new ObjectResult(new ProblemDetails { Status = status, Detail = error.Message })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
