namespace ASPHotelM2631.Application.Rooms;

public interface IRoomSearchService
{
    Task<SearchOutcome> SearchAsync(RoomSearchRequest request);
    Task<RoomDto?> GetAsync(int id);
    Task<IReadOnlyList<RoomTypeDto>> GetRoomTypesAsync();
}
