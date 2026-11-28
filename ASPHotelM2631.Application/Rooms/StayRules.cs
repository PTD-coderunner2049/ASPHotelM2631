namespace ASPHotelM2631.Application.Rooms;

public static class StayRules
{
    public const int MaxNights = 30;

    // "Hôm nay" theo giờ Việt Nam, không phụ thuộc múi giờ của máy chủ
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    /// <returns>null nếu hợp lệ, ngược lại là thông báo lỗi.</returns>
    public static string? Validate(DateOnly? checkIn, DateOnly? checkOut, int? guests, out int nights)
    {
        nights = 0;
        if ((checkIn is null) != (checkOut is null))
            return "Cần nhập cả ngày nhận phòng và ngày trả phòng.";
        if (guests is < 1 or > 15)
            return "Số khách phải từ 1 đến 15.";

        if (checkIn is { } i && checkOut is { } o)
        {
            if (i < Today()) return "Ngày nhận phòng không được ở quá khứ.";
            if (o <= i) return "Ngày trả phòng phải sau ngày nhận phòng.";
            nights = o.DayNumber - i.DayNumber;
            if (nights > MaxNights) return $"Mỗi lần chỉ tìm tối đa {MaxNights} đêm.";
        }
        return null;
    }
}