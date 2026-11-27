namespace ASPHotelM2631.Domain.Bookings;

public enum RoomCondition { Available, Occupied, Cleaning, Maintenance }
public enum RoomTypeStatus { Active, Inactive }

public sealed class RoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal BaseNightlyRate { get; set; }
    public RoomTypeStatus Status { get; set; } = RoomTypeStatus.Active;
    public List<SeasonalRate> Rates { get; set; } = [];

    public decimal Quote(Stay stay)
    {
        var rates = Rates.Where(rate => rate.Start < stay.CheckOut && stay.CheckIn <= rate.End)
            .OrderByDescending(rate => rate.Start)
            .ThenByDescending(rate => rate.Id)
            .ToArray();
        var boundaries = new SortedSet<DateOnly> { stay.CheckIn, stay.CheckOut };
        foreach (var rate in rates)
        {
            if (rate.Start > stay.CheckIn)
                boundaries.Add(rate.Start);
            if (rate.End < stay.CheckOut)
                boundaries.Add(rate.End.AddDays(1));
        }

        var total = 0m;
        var from = stay.CheckIn;
        foreach (var to in boundaries.Skip(1))
        {
            var nightlyRate = rates.FirstOrDefault(rate => rate.Start <= from && from <= rate.End)?.NightlyRate
                ?? BaseNightlyRate;
            total += (to.DayNumber - from.DayNumber) * nightlyRate;
            from = to;
        }
        return total;
    }
}

public sealed class Room
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int Floor { get; set; }
    public int RoomTypeId { get; set; }
    public RoomType Type { get; set; } = null!;
    public RoomCondition Condition { get; set; }
}

public sealed class SeasonalRate
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public decimal NightlyRate { get; set; }
}
