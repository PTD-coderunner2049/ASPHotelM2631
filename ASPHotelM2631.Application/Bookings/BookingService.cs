using ASPHotelM2631.Domain.Bookings;

namespace ASPHotelM2631.Application.Bookings;

public sealed class BookingService(IBookingStore bookings, GuestService guests, TimeProvider clock)
{
    private static readonly TimeOnly HotelCheckIn = new(14, 0);
    private static readonly TimeSpan HotelUtcOffset = TimeSpan.FromHours(7);

    public async Task<IReadOnlyList<AvailableRoom>> FindAvailableAsync(DateOnly checkIn, DateOnly checkOut,
        int? roomTypeId, CancellationToken cancellationToken)
    {
        var stay = ValidateStay(checkIn, checkOut);
        if (roomTypeId is <= 0)
            throw new BookingException(BookingFailure.Invalid, "Mã loại phòng không hợp lệ.");

        var rooms = await bookings.FindAvailableRoomsAsync(stay, roomTypeId, cancellationToken);
        var quotes = rooms.Select(room => room.Type).DistinctBy(type => type.Id)
            .ToDictionary(type => type.Id, type => type.Quote(stay));
        return rooms.Select(room => new AvailableRoom(room.Id, room.Number, room.Floor,
            room.RoomTypeId, room.Type.Name, quotes[room.RoomTypeId])).ToArray();
    }

    public async Task<BookingDetails> GetAsync(int id, CancellationToken cancellationToken) =>
        ToDetails(await GetBookingAsync(id, cancellationToken));

    public Task<Page<BookingDetails>> SearchAsync(BookingSearch search, CancellationToken cancellationToken)
    {
        ValidatePage(search.Page, search.PageSize);
        if (search.Status is { } status && !Enum.IsDefined(status))
            throw new BookingException(BookingFailure.Invalid, "Trạng thái đặt phòng không hợp lệ.");
        if (search.From.HasValue != search.To.HasValue)
            throw new BookingException(BookingFailure.Invalid, "Cần nhập cả hai mốc thời gian tìm kiếm.");
        if (search.From is { } from && search.To is { } to)
            ValidateStay(from, to);

        return bookings.FindBookingsAsync(search with { Search = search.Search?.Trim() }, cancellationToken);
    }

    public async Task<BookingDetails> CreateAsync(BookingInput input, string userId, CancellationToken cancellationToken)
    {
        var (stay, channel) = ValidateInput(input);
        var booking = await bookings.SaveBookingAsync(async token =>
        {
            var guest = await guests.GetGuestAsync(input.GuestId, token);
            var room = await FindBookableRoomAsync(input.RoomId, stay, null, token);
            var reservation = Booking.Create(guest, room, stay, input.GuestCount, channel, room.Type.Quote(stay), userId);
            bookings.Add(reservation);
            return reservation;
        }, cancellationToken);
        return ToDetails(booking);
    }

    public async Task<BookingDetails> AmendAsync(int id, BookingInput input, CancellationToken cancellationToken)
    {
        var (stay, channel) = ValidateInput(input);
        var booking = await bookings.SaveBookingAsync(async token =>
        {
            var current = await GetBookingAsync(id, token);
            if (!current.CanAmend)
                throw new BookingException(BookingFailure.Conflict, "Trạng thái hiện tại không cho phép sửa đặt phòng.");

            var guest = await guests.GetGuestAsync(input.GuestId, token);
            var total = current.Total;
            var room = current.Room;
            if (current.RoomId != input.RoomId || current.CheckIn != stay.CheckIn || current.CheckOut != stay.CheckOut)
            {
                room = await FindBookableRoomAsync(input.RoomId, stay, current.Id, token);
                total = room.Type.Quote(stay);
            }
            if (total < current.PaidAmount)
                throw new BookingException(BookingFailure.Conflict, "Giá mới thấp hơn số tiền đã ghi nhận.");

            current.Amend(guest, room, stay, input.GuestCount, channel, total);
            return current;
        }, cancellationToken);
        return ToDetails(booking);
    }

    public async Task<BookingDetails> ConfirmAsync(int id, decimal paidAmount, CancellationToken cancellationToken)
    {
        if (paidAmount < 0 || decimal.Round(paidAmount, 2) != paidAmount)
            throw new BookingException(BookingFailure.Invalid, "Số tiền đã thu phải hợp lệ.");

        var booking = await bookings.SaveBookingAsync(async token =>
        {
            var current = await GetBookingAsync(id, token);
            if (current.Status == BookingStatus.Confirmed && current.PaidAmount == paidAmount)
                return current;
            if (current.Status != BookingStatus.Pending)
                throw new BookingException(BookingFailure.Conflict, "Chỉ có thể xác nhận đặt phòng đang chờ.");
            if (paidAmount > current.Total)
                throw new BookingException(BookingFailure.Invalid, "Số tiền đã thu vượt tổng tiền phòng.");

            await FindBookableRoomAsync(current.RoomId, new Stay(current.CheckIn, current.CheckOut), current.Id, token);
            current.Confirm(paidAmount);
            return current;
        }, cancellationToken);
        return ToDetails(booking);
    }

    public async Task<BookingDetails> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var booking = await bookings.SaveBookingAsync(async token =>
        {
            var current = await GetBookingAsync(id, token);
            if (current.Status == BookingStatus.Cancelled)
                return current;
            if (!current.CanAmend)
                throw new BookingException(BookingFailure.Conflict, "Trạng thái hiện tại không cho phép hủy đặt phòng.");

            var checkIn = new DateTimeOffset(current.CheckIn.ToDateTime(HotelCheckIn), HotelUtcOffset);
            current.Cancel(clock.GetUtcNow(), checkIn);
            return current;
        }, cancellationToken);
        return ToDetails(booking);
    }

    internal static void ValidatePage(int number, int size)
    {
        if (size is < 1 or > 100 || number < 1 || number > int.MaxValue / size)
            throw new BookingException(BookingFailure.Invalid, "Trang phải từ 1 và kích thước trang từ 1 đến 100.");
    }

    internal static BookingDetails ToDetails(Booking booking) =>
        new(booking.Id, booking.GuestId, booking.Guest.FullName, booking.RoomId, booking.Room.Number,
            booking.CheckIn, booking.CheckOut, booking.GuestCount, booking.Channel.ToString(),
            booking.Status.ToString(), booking.Total, booking.PaidAmount, booking.RefundAmount, booking.CancelledAt);

    private async Task<Booking> GetBookingAsync(int id, CancellationToken cancellationToken) =>
        await bookings.FindBookingAsync(id, cancellationToken)
        ?? throw new BookingException(BookingFailure.NotFound, "Không tìm thấy đặt phòng.");

    private async Task<Room> FindBookableRoomAsync(int roomId, Stay stay, int? exceptBooking,
        CancellationToken cancellationToken)
    {
        var room = await bookings.FindRoomAsync(roomId, cancellationToken)
            ?? throw new BookingException(BookingFailure.NotFound, "Không tìm thấy phòng.");
        if (room.Type.Status != RoomTypeStatus.Active || room.Condition == RoomCondition.Maintenance ||
            await bookings.IsRoomBlockedAsync(roomId, stay, exceptBooking, cancellationToken))
            throw new BookingException(BookingFailure.Conflict, "Phòng không khả dụng trong khoảng ngày đã chọn.");
        return room;
    }

    private static (Stay Stay, BookingChannel Channel) ValidateInput(BookingInput input)
    {
        if (input.GuestId <= 0 || input.RoomId <= 0 || input.GuestCount <= 0)
            throw new BookingException(BookingFailure.Invalid, "Cần chọn khách, phòng và số khách hợp lệ.");
        if (!Enum.TryParse<BookingChannel>(input.Channel, true, out var channel) ||
            !string.Equals(channel.ToString(), input.Channel?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BookingException(BookingFailure.Invalid, "Kênh đặt phòng phải là WalkIn hoặc Phone.");
        return (ValidateStay(input.CheckIn, input.CheckOut), channel);
    }

    private static Stay ValidateStay(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkIn == default || checkOut <= checkIn)
            throw new BookingException(BookingFailure.Invalid, "Ngày trả phòng phải sau ngày nhận phòng.");
        return new Stay(checkIn, checkOut);
    }
}
