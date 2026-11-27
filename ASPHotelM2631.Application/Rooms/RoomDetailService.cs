namespace ASPHotelM2631.Application.Rooms;

public enum DetailStatus { Ok, NotFound, Invalid }

public sealed record DetailResult<T>(DetailStatus Status, T? Data = default, string? Error = null);

public sealed class RoomDetailService(IRoomTypeQueries types, IRoomStaffQueries rooms) : IRoomDetailService
{
    public async Task<DetailResult<RoomTypeDetailDto>> GetRoomTypeAsync(int id, RoomTypeDetailRequest r)
    {
        var error = StayRules.Validate(r.CheckIn, r.CheckOut, r.Guests, out var nights);
        if (error is not null) return new(DetailStatus.Invalid, Error: error);

        var t = await types.GetActiveAsync(id);
        if (t is null) return new(DetailStatus.NotFound);

        AvailabilityDto? availability = null;
        if (r.CheckIn is { } checkIn && r.CheckOut is { } checkOut)
        {
            var left = await types.CountAvailableAsync(id, checkIn, checkOut);
            availability = new AvailabilityDto(
                checkIn, checkOut, nights,
                TotalPrice: t.BasePrice * nights,
                AvailableRooms: left,
                CanAccommodate: r.Guests is null || r.Guests <= t.MaxGuests);
        }

        var amenities = (t.Amenities ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new(DetailStatus.Ok, new RoomTypeDetailDto(
            t.Id, t.Name, t.Description, amenities, t.ImageUrl, t.MaxGuests, t.BasePrice, availability));
    }

    public async Task<DetailResult<RoomStaffDetailDto>> GetRoomForStaffAsync(int roomId)
    {
        var room = await rooms.GetAsync(roomId, StayRules.Today());
        return room is null ? new(DetailStatus.NotFound) : new(DetailStatus.Ok, room);
    }
}