using ASPHotelM2631.Application.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ASPHotelM2631.Web.Controllers.Rooms;

/// <summary>API tìm phòng công khai: không cần đăng nhập.</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-search")]
[Route("api")]
public sealed class RoomsController(IRoomSearchService rooms) : ControllerBase
{
    [HttpGet("rooms")]
    public async Task<IActionResult> Search([FromQuery] RoomSearchRequest request)
    {
        var outcome = await rooms.SearchAsync(request);
        return outcome.Error is null ? Ok(outcome.Data) : BadRequest(new { message = outcome.Error });
    }

    [HttpGet("rooms/{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var room = await rooms.GetAsync(id);
        return room is null ? NotFound(new { message = "Không tìm thấy phòng." }) : Ok(room);
    }

    [HttpGet("room-types")]
    public async Task<IActionResult> RoomTypes() => Ok(await rooms.GetRoomTypesAsync());
}