namespace ASPHotelM2631.Application.Rooms;

public sealed class RoomTypeDetailRequest
{
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int? Guests { get; set; }
}

// Dữ liệu thô từ database (Infrastructure trả về, service dựng thành DTO)
public sealed record RoomTypeRecord(
    int Id, string Name, string? Description, string? Amenities,
    string? ImageUrl, int MaxGuests, decimal BasePrice);

public sealed record AvailabilityDto(
    DateOnly CheckIn, DateOnly CheckOut, int Nights,
    decimal TotalPrice, int AvailableRooms, bool CanAccommodate);

public sealed record RoomTypeDetailDto(
    int RoomTypeId, string Name, string? Description, IReadOnlyList<string> Amenities,
    string? ImageUrl, int MaxGuests, decimal BasePrice,
    AvailabilityDto? Availability);   // null nếu không truyền ngày

// ----- Dành cho nhân viên -----
public sealed record RoomTypeSummaryDto(int RoomTypeId, string Name, int MaxGuests, decimal BasePrice);

public sealed record RoomStayDto(int BookingId, string BookingCode, DateOnly CheckIn, DateOnly CheckOut, string Status);

public sealed record RoomStaffDetailDto(
    int RoomId, string RoomNumber, int Floor, string Status,
    RoomTypeSummaryDto RoomType,
    RoomStayDto? CurrentStay,                    // đang có khách (CheckedIn)
    IReadOnlyList<RoomStayDto> Upcoming);        // đã gán phòng, sắp tới