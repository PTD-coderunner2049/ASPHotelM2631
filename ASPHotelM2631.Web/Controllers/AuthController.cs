using Microsoft.AspNetCore.Mvc;
namespace ASPHotelM2631.Web.Controllers;
public sealed class AuthController : Controller { [HttpGet] public IActionResult Login() => View(); [HttpGet] public IActionResult Register() => View(); }
