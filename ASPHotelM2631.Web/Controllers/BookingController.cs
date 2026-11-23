using Microsoft.AspNetCore.Mvc;

namespace ASPHotelM2631.Web.Controllers;

public sealed class BookingController : Controller
{
    // Màn hình tìm và đặt phòng hiện dùng dữ liệu demo ở JavaScript.
    // Khi có API, controller này sẽ gọi Application layer thay vì trả View trực tiếp.
    public IActionResult Search() => View();
}
