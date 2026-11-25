namespace ASPHotelM2631.Application.Rooms;

public interface IRoomQueries
{
    Task<PagedResult<RoomDto>> SearchAsync(RoomSearchRequest request);
    Task<RoomDto?> GetAsync(int id);
    Task<IReadOnlyList<RoomTypeDto>> GetRoomTypesAsync();
}