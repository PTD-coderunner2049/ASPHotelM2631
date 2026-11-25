namespace ASPHotelM2631.Application.Rooms;

public interface IRoomSearchService
{
    Task<SearchOutcome> SearchAsync(RoomSearchRequest request);
    Task<RoomDto?> GetAsync(int id);
    Task<IReadOnlyList<RoomTypeDto>> GetRoomTypesAsync();
}

public sealed class RoomSearchService(IRoomQueries queries) : IRoomSearchService
{
    private const int MaxNights = 30;
    private const int MaxPageSize = 50;

    public async Task<SearchOutcome> SearchAsync(RoomSearchRequest r)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if ((r.CheckIn is null) != (r.CheckOut is null))
            return Fail("Cần nhập cả ngày nhận phòng và ngày trả phòng.");

        var nights = 0;
        if (r.CheckIn is { } checkIn && r.CheckOut is { } checkOut)
        {
            if (checkIn < today) return Fail("Ngày nhận phòng không được ở quá khứ.");
            if (checkOut <= checkIn) return Fail("Ngày trả phòng phải sau ngày nhận phòng.");
            nights = checkOut.DayNumber - checkIn.DayNumber;
            if (nights > MaxNights) return Fail($"Mỗi lần chỉ tìm tối đa {MaxNights} đêm.");
        }

        if (r.Guests is < 1 or > 20) return Fail("Số khách phải từ 1 đến 20.");
        if (r.MinPrice is < 0 || r.MaxPrice is < 0) return Fail("Giá không được âm.");
        if (r.MinPrice > r.MaxPrice) return Fail("Giá thấp nhất không được lớn hơn giá cao nhất.");
        if (r.Page < 1 || r.PageSize < 1 || r.PageSize > MaxPageSize)
            return Fail($"Trang phải từ 1 và kích thước trang từ 1 đến {MaxPageSize}.");

        var page = await queries.SearchAsync(r);

        if (nights > 0)   // có ngày thì tính luôn tổng tiền cho cả kỳ lưu trú
            page = page with
            {
                Items = page.Items
                    .Select(x => x with { TotalPrice = x.RoomType.PricePerNight * nights })
                    .ToList()
            };

        return new SearchOutcome(page, null);
    }

    public Task<RoomDto?> GetAsync(int id) => queries.GetAsync(id);
    public Task<IReadOnlyList<RoomTypeDto>> GetRoomTypesAsync() => queries.GetRoomTypesAsync();

    private static SearchOutcome Fail(string message) => new(null, message);
}