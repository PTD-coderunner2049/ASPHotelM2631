using ASPHotelM2631.Application.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ASPHotelM2631.Web.Controllers.Rooms;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-search")]   // dùng chính sách giới hạn đã tạo ở bài tìm phòng
[Route("api")]
public sealed class RoomTypesController(IRoomDetailService details) : ControllerBase
{
    [HttpGet("room-types/{id:int}")]
    public async Task<IActionResult> Get(int id, [FromQuery] RoomTypeDetailRequest request)
    {
        var result = await details.GetRoomTypeAsync(id, request);
        return result.Status switch
        {
            DetailStatus.Ok => Ok(result.Data),
            DetailStatus.NotFound => NotFound(new { message = "Không tìm thấy loại phòng." }),
            _ => BadRequest(new { message = result.Error })
        };
    }
}