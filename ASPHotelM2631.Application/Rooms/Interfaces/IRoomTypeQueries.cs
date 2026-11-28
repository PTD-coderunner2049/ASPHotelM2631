namespace ASPHotelM2631.Application.Rooms;

public interface IRoomTypeQueries
{
    Task<RoomTypeRecord?> GetActiveAsync(int id);

    /// <summary>Số phòng còn trống ít nhất trong các đêm của kỳ lưu trú (tính từng đêm).</summary>
    Task<int> CountAvailableAsync(int roomTypeId, DateOnly checkIn, DateOnly checkOut);
}

