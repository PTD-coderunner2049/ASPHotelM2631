namespace ASPHotelM2631.Domain.Entities;

public enum RoomStatus { Available, Maintenance }

public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = "";
    public int Floor { get; set; }
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public bool IsActive { get; set; } = true;
}
