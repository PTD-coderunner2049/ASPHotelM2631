using Microsoft.AspNetCore.Mvc;
namespace ASPHotelM2631.Web.Controllers;
public sealed class AccessController : Controller { public IActionResult Forbidden() => View(); }
