namespace ASPHotelM2631.Application.Rooms;

public interface IRoomDetailService
{
    Task<DetailResult<RoomTypeDetailDto>> GetRoomTypeAsync(int id, RoomTypeDetailRequest request);
    Task<DetailResult<RoomStaffDetailDto>> GetRoomForStaffAsync(int roomId);
}
