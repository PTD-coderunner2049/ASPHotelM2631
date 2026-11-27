using ASPHotelM2631.Domain.Bookings;
using Xunit;

namespace ASPHotelM2631.Tests.Unit;

public sealed class RoomQuoteTests
{
    [TcFact("TC-UNIT-FR3-001", "Tính giá theo từng khoảng mùa vụ", Req = "F32,F33",
        Expected = "Tính cả đêm cuối mùa, không tính đêm trả, ưu tiên mùa bắt đầu sau.")]
    public void QuoteCombinesSeasonalAndBaseRates()
    {
        var start = new DateOnly(2026, 12, 1);
        var roomType = new RoomType
        {
            BaseNightlyRate = 100,
            Rates =
            [
                new() { Id = 1, Start = start.AddDays(1), End = start.AddDays(2), NightlyRate = 150 },
                new() { Id = 2, Start = start.AddDays(2), End = start.AddDays(2), NightlyRate = 200 },
                new() { Id = 3, Start = start.AddDays(4), End = start.AddDays(7), NightlyRate = 999 }
            ]
        };

        Assert.Equal(550, roomType.Quote(new Stay(start, start.AddDays(4))));
    }
}
