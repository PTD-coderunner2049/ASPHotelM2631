namespace ASPHotelM2631.Application.Rooms;

public sealed class RoomSearchRequest
{
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int? Guests { get; set; }
    public int? RoomTypeId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record RoomTypeDto(
    int Id,
    string Name,
    string? Description,
    string Amenities,
    string? ImageUrl,
    int MaxGuests,
    decimal PricePerNight,
    bool IsActive);

public sealed record RoomDto(int Id, string Number, int Floor, RoomTypeDto RoomType, decimal? TotalPrice);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public sealed record SearchOutcome(PagedResult<RoomDto>? Data, string? Error);
