using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ASPHotelM2631.Application.Bookings;
using ASPHotelM2631.Domain.Bookings;
using ASPHotelM2631.Domain.Constants;
using ASPHotelM2631.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPHotelM2631.Tests.Api;

public sealed class CounterBookingTests : IDisposable
{
    private const string Guests = "/api/reception/guests";
    private const string Bookings = "/api/reception/bookings";
    private const string Availability = "/api/reception/rooms/availability";
    private static readonly DateOnly Arrival = new(2026, 12, 6);
    private static readonly DateOnly Departure = Arrival.AddDays(2);
    private static readonly DateTimeOffset RefundDeadline = new(2026, 12, 5, 7, 0, 0, TimeSpan.Zero);
    private readonly HotelApiFactory _factory;
    private readonly HttpClient _client;
    private readonly TestClock _clock = new() { Now = RefundDeadline };

    public CounterBookingTests()
    {
        _factory = new HotelApiFactory { Clock = _clock };
        _client = _factory.Https();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var standard = new RoomType
        {
            Id = 1, Name = "Standard", BaseNightlyRate = 100,
            Rates = [new SeasonalRate { Id = 1, Start = Arrival, End = Arrival, NightlyRate = 150 }]
        };
        var deluxe = new RoomType { Id = 2, Name = "Deluxe", BaseNightlyRate = 200 };
        var inactive = new RoomType { Id = 3, Name = "Inactive", Status = RoomTypeStatus.Inactive, BaseNightlyRate = 300 };
        db.Rooms.AddRange(
            new Room { Id = 1, Number = "101", Floor = 1, Type = standard },
            new Room { Id = 2, Number = "102", Floor = 1, Type = standard },
            new Room { Id = 3, Number = "201", Floor = 2, Type = deluxe, Condition = RoomCondition.Maintenance },
            new Room { Id = 4, Number = "301", Floor = 3, Type = inactive });
        db.SaveChanges();
    }

    [TcFact("TC-API-FR3-001", "FR3 yêu cầu quyền lễ tân", Req = "FR3",
        Expected = "Ẩn danh nhận 401, role khách nhận 403, lễ tân nhận 200.")]
    public async Task ReceptionRoutesCheckJwtAndRole()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Guests)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(SearchUrl(Arrival, Departure))).StatusCode);
        await AuthorizeAsync(RoleNames.Customer);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(Bookings)).StatusCode);
        await AuthorizeAsync();
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Bookings)).StatusCode);
    }

    [TcFact("TC-API-FR3-002", "Tạo, sửa và tra cứu hồ sơ khách", Req = "F31",
        Expected = "201 khi tạo, dữ liệu sửa có thể xem và tìm, email sai nhận 400.")]
    public async Task GuestProfilesCanBeCreatedUpdatedAndSearched()
    {
        await AuthorizeAsync();
        var guest = await CreateGuestAsync();
        Assert.Equal("Guest Test", guest.FullName);
        using var updated = await _client.PutAsJsonAsync($"{Guests}/{guest.Id}",
            new GuestInput("Guest Updated", "+84 901 000 002", "guest@example.test"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var details = await _client.GetFromJsonAsync<GuestDetails>($"{Guests}/{guest.Id}");
        Assert.Equal("guest@example.test", details!.Email);
        var search = await _client.GetFromJsonAsync<Page<GuestDetails>>($"{Guests}?search=Updated&pageSize=1");
        Assert.Equal(guest.Id, Assert.Single(search!.Items).Id);
        Assert.Equal(1, search.Total);
        using var invalid = await _client.PostAsJsonAsync(Guests, new GuestInput("Guest", "0900", "bad email"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{Guests}/9000")).StatusCode);
    }

    [TcFact("TC-API-FR3-003", "F32 lọc phòng, trùng kỳ ở và giá mùa vụ", Req = "F32",
        Expected = "Loại phòng bảo trì/ngừng hoạt động và phòng trùng ngày, giữ hai kỳ liền kề.")]
    public async Task AvailabilityUsesRoomStateAndHalfOpenStay()
    {
        await AuthorizeAsync();
        var guest = await CreateGuestAsync();
        var initial = await FindRoomsAsync(Arrival, Departure);
        Assert.Equal([1, 2], initial.Select(room => room.Id));
        Assert.All(initial, room => Assert.Equal(250, room.Total));
        Assert.Empty(await FindRoomsAsync(Arrival, Departure, roomTypeId: 2));
        Assert.Empty(await FindRoomsAsync(Arrival, Departure, roomTypeId: 3));

        await CreateBookingAsync(Input(guest.Id));
        Assert.Equal(2, Assert.Single(await FindRoomsAsync(Arrival, Departure, roomTypeId: 1)).Id);
        Assert.Equal(2, (await FindRoomsAsync(Departure, Departure.AddDays(1))).Length);
        Assert.Equal(2, (await FindRoomsAsync(Arrival.AddDays(-1), Arrival)).Length);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync(SearchUrl(Arrival, Arrival))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{Availability}?checkIn=2026-12-06")).StatusCode);
    }

    [TcFact("TC-API-FR3-004", "Luồng đặt phòng tại quầy từ tạo đến hủy", Req = "F33,F34,F35,F36,F37",
        Expected = "Tạo chờ, xác nhận, đổi phòng, tìm thấy, hủy và giải phóng phòng.")]
    public async Task CounterBookingLifecycle()
    {
        await AuthorizeAsync();
        var guest = await CreateGuestAsync();
        var input = Input(guest.Id);
        var created = await CreateBookingAsync(input);
        Assert.Equal("Pending", created.Status);
        Assert.Equal(250, created.Total);

        var confirmed = await ConfirmAsync(created.Id, 100);
        Assert.Equal("Confirmed", confirmed.Status);
        Assert.Equal(100, confirmed.PaidAmount);
        Assert.Equal(confirmed, await ConfirmAsync(created.Id, 100));

        using var updated = await _client.PutAsJsonAsync($"{Bookings}/{created.Id}", input with
        {
            RoomId = 2, GuestCount = 3, Channel = "Phone"
        });
        var edited = await ReadAsync<BookingDetails>(updated, HttpStatusCode.OK);
        Assert.Equal((2, 3, "Phone", "Confirmed"), (edited.RoomId, edited.GuestCount, edited.Channel, edited.Status));
        Assert.Equal(edited, await _client.GetFromJsonAsync<BookingDetails>($"{Bookings}/{created.Id}"));
        var page = await _client.GetFromJsonAsync<Page<BookingDetails>>(
            $"{Bookings}?status=Confirmed&search=102&from=2026-12-06&to=2026-12-08&page=1&pageSize=1");
        Assert.Equal(edited.Id, Assert.Single(page!.Items).Id);
        Assert.Equal(1, page.Total);

        var cancelled = await CancelAsync(created.Id);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(100, cancelled.RefundAmount);
        Assert.Equal(cancelled, await CancelAsync(created.Id));
        Assert.Equal(2, (await FindRoomsAsync(Arrival, Departure)).Length);
        using var amendCancelled = await _client.PutAsJsonAsync($"{Bookings}/{created.Id}", input);
        Assert.Equal(HttpStatusCode.Conflict, amendCancelled.StatusCode);
    }

    [TcFact("TC-API-FR3-005", "Một phòng không nhận hai lượt đặt trùng ngày", Req = "F33",
        Expected = "Một 201, một 409 và chỉ một hàng đặt phòng.")]
    public async Task ExistingBookingRejectsSecondRequest()
    {
        await AuthorizeAsync();
        var guest = await CreateGuestAsync();
        var input = Input(guest.Id);
        await CreateBookingAsync(input);
        using var conflicting = await _client.PostAsJsonAsync(Bookings, input);
        Assert.Equal(HttpStatusCode.Conflict, conflicting.StatusCode);
        var page = await _client.GetFromJsonAsync<Page<BookingDetails>>(Bookings);
        Assert.Equal(1, page!.Total);
        Assert.Single(page.Items);
    }

    [TcTheory("TC-API-FR3-006", "Hoàn tiền ở ba mốc quanh 24 giờ", Req = "F36",
        Expected = "Hoàn số tiền đã thu nếu còn ít nhất 24 giờ.")]
    [InlineData(-1, 100)]
    [InlineData(0, 100)]
    [InlineData(1, 0)]
    public async Task RefundUsesTheHotelTimeAndPaidAmount(int secondsFromDeadline, int refund)
    {
        _clock.Now = RefundDeadline.AddSeconds(secondsFromDeadline);
        await AuthorizeAsync();
        var guest = await CreateGuestAsync();
        var booking = await CreateBookingAsync(Input(guest.Id));
        await ConfirmAsync(booking.Id, 100);
        var cancelled = await CancelAsync(booking.Id);
        Assert.Equal(refund, cancelled.RefundAmount);
        Assert.Equal(_clock.Now, cancelled.CancelledAt);
    }

    [TcFact("TC-API-FR3-007", "Sửa trùng phòng và thanh toán sai giữ nguyên dữ liệu", Req = "F33,F34,F35,F37",
        Expected = "Trả 400/409, giữ nguyên booking và giá.")]
    public async Task InvalidActionsDoNotPersistChanges()
    {
        await AuthorizeAsync();
        var guest = await CreateGuestAsync();
        var input = Input(guest.Id);
        var first = await CreateBookingAsync(input);
        var second = await CreateBookingAsync(input with { RoomId = 2 });
        using var conflicting = await _client.PutAsJsonAsync($"{Bookings}/{second.Id}", input);
        Assert.Equal(HttpStatusCode.Conflict, conflicting.StatusCode);
        using var invalidPayment = await _client.PostAsJsonAsync($"{Bookings}/{first.Id}/confirm", new { paidAmount = 251 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPayment.StatusCode);
        using var invalidRoom = await _client.PostAsJsonAsync(Bookings, input with { RoomId = 3 });
        Assert.Equal(HttpStatusCode.Conflict, invalidRoom.StatusCode);
        Assert.Equal(second, await _client.GetFromJsonAsync<BookingDetails>($"{Bookings}/{second.Id}"));
        Assert.Equal(first, await _client.GetFromJsonAsync<BookingDetails>($"{Bookings}/{first.Id}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{Bookings}?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{Bookings}?from=2026-12-06")).StatusCode);
    }

    private async Task AuthorizeAsync(string role = RoleNames.Receptionist)
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var token = await _factory.TokenForRole(_client, role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<GuestDetails> CreateGuestAsync()
    {
        using var response = await _client.PostAsJsonAsync(Guests, new GuestInput(" Guest Test ", "0901000001", null));
        return await ReadAsync<GuestDetails>(response, HttpStatusCode.Created);
    }

    private async Task<BookingDetails> CreateBookingAsync(BookingInput input)
    {
        using var response = await _client.PostAsJsonAsync(Bookings, input);
        var booking = await ReadAsync<BookingDetails>(response, HttpStatusCode.Created);
        Assert.EndsWith($"{Bookings}/{booking.Id}", response.Headers.Location!.ToString());
        return booking;
    }

    private async Task<BookingDetails> ConfirmAsync(int id, decimal paidAmount)
    {
        using var response = await _client.PostAsJsonAsync($"{Bookings}/{id}/confirm", new { paidAmount });
        return await ReadAsync<BookingDetails>(response, HttpStatusCode.OK);
    }

    private async Task<BookingDetails> CancelAsync(int id)
    {
        using var response = await _client.PostAsync($"{Bookings}/{id}/cancel", null);
        return await ReadAsync<BookingDetails>(response, HttpStatusCode.OK);
    }

    private static BookingInput Input(int guestId) => new(guestId, 1, Arrival, Departure, 2, "WalkIn");

    private async Task<AvailableRoom[]> FindRoomsAsync(DateOnly from, DateOnly to, int? roomTypeId = null) =>
        (await _client.GetFromJsonAsync<AvailableRoom[]>(SearchUrl(from, to) + $"&roomTypeId={roomTypeId}"))!;

    private static string SearchUrl(DateOnly from, DateOnly to) => $"{Availability}?checkIn={from:yyyy-MM-dd}&checkOut={to:yyyy-MM-dd}";

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
