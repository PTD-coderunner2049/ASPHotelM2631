using ASPHotelM2631.Application.Rooms;
using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers.Rooms;

[ApiController]
[Authorize(Roles = $"{RoleNames.Receptionist},{RoleNames.Housekeeping},{RoleNames.Manager},{RoleNames.Admin}")]
[Route("api/staff/rooms")]
public sealed class StaffRoomsController(IRoomDetailService details) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await details.GetRoomForStaffAsync(id);
        return result.Status == DetailStatus.Ok
            ? Ok(result.Data)
            : NotFound(new { message = "Không tìm thấy phòng." });
    }
}
