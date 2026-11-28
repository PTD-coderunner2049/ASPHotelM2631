namespace ASPHotelM2631.Application.Rooms;

public interface IRoomStaffQueries
{
    Task<RoomStaffDetailDto?> GetAsync(int roomId, DateOnly today);
}

