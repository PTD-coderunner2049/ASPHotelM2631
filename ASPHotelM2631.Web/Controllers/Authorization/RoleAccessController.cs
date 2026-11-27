using ASPHotelM2631.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers.Authorization;

/// <summary>Route thử phân quyền. Sai vai trò -> 403, chưa đăng nhập -> 401.</summary>
[ApiController]
[Route("api")]
public sealed class RoleAccessController : ControllerBase
{
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("customer")] public IActionResult Customer() => Ok(new { area = "customer" });

    [Authorize(Roles = RoleNames.Receptionist)]
    [HttpGet("reception")] public IActionResult Reception() => Ok(new { area = "reception" });

    [Authorize(Roles = RoleNames.Housekeeping)]
    [HttpGet("housekeeping")] public IActionResult Housekeeping() => Ok(new { area = "housekeeping" });

    [Authorize(Roles = RoleNames.Accountant)]
    [HttpGet("accounting")] public IActionResult Accounting() => Ok(new { area = "accounting" });

    [Authorize(Roles = RoleNames.Manager)]
    [HttpGet("manager")] public IActionResult Manager() => Ok(new { area = "manager" });

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet("admin")] public IActionResult Admin() => Ok(new { area = "admin" });
}