namespace ASPHotelM2631.Domain.Entities;

public class RoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Amenities { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int MaxGuests { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Room> Rooms { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
}
