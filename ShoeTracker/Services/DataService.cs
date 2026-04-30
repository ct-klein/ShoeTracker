using System.Text.Json;
using ShoeTracker.Models;

namespace ShoeTracker.Services;

public sealed class DataService
{
    private static readonly string DataDir   = Path.Combine(AppContext.BaseDirectory, "data");
    private static readonly string ShoesPath = Path.Combine(DataDir, "shoes.json");
    private static readonly string RunsPath  = Path.Combine(DataDir, "runs.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented        = true,
        Converters           = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public List<Shoe>     Shoes { get; private set; } = [];
    public List<RunEntry> Runs  { get; private set; } = [];

    public void Load()
    {
        Directory.CreateDirectory(DataDir);
        try
        {
            if (File.Exists(ShoesPath))
                Shoes = JsonSerializer.Deserialize<List<Shoe>>(File.ReadAllText(ShoesPath), JsonOpts) ?? [];
        }
        catch { /* corrupt data — start fresh */ }

        try
        {
            if (File.Exists(RunsPath))
                Runs = JsonSerializer.Deserialize<List<RunEntry>>(File.ReadAllText(RunsPath), JsonOpts) ?? [];
        }
        catch { /* corrupt data — start fresh */ }
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        try { File.WriteAllText(ShoesPath, JsonSerializer.Serialize(Shoes, JsonOpts)); } catch { }
        try { File.WriteAllText(RunsPath,  JsonSerializer.Serialize(Runs,  JsonOpts)); } catch { }
    }

    public void AddShoe(Shoe shoe)
    {
        Shoes.Add(shoe);
        Save();
    }

    public void UpdateShoe(Shoe shoe)
    {
        var idx = Shoes.FindIndex(s => s.Id == shoe.Id);
        if (idx >= 0) Shoes[idx] = shoe;
        Save();
    }

    public void DeleteShoe(Guid id)
    {
        Shoes.RemoveAll(s => s.Id == id);
        Runs.RemoveAll(r => r.ShoeId == id);
        Save();
    }

    public void AddRun(RunEntry run)
    {
        Runs.Add(run);
        Save();
    }

    public void DeleteRun(Guid id)
    {
        Runs.RemoveAll(r => r.Id == id);
        Save();
    }

    public ShoeStats GetShoeStats(Shoe shoe)
    {
        var shoeRuns = Runs.Where(r => r.ShoeId == shoe.Id).ToList();
        double total   = shoeRuns.Sum(r => r.Distance);
        int    count   = shoeRuns.Count;
        double avg     = count > 0 ? total / count : 0;
        double longest = shoeRuns.Count > 0 ? shoeRuns.Max(r => r.Distance) : 0;
        double worn    = shoe.MaxMileage > 0 ? Math.Min(100, total / shoe.MaxMileage * 100) : 0;
        double remain  = Math.Max(0, shoe.MaxMileage - total);

        DateTime? lastRun = shoeRuns.Count > 0
            ? shoeRuns.Max(r => r.Date)
            : null;

        double? avgPace = null;
        var timedRuns = shoeRuns.Where(r => r.DurationMinutes.HasValue && r.Distance > 0).ToList();
        if (timedRuns.Count > 0)
            avgPace = timedRuns.Sum(r => r.DurationMinutes!.Value) / timedRuns.Sum(r => r.Distance);

        DateTime? projected = null;
        if (remain > 0 && shoeRuns.Count >= 3)
        {
            var oldest = shoeRuns.Min(r => r.Date);
            double weeks = Math.Max(1, (DateTime.Today - oldest).TotalDays / 7.0);
            double milesPerWeek = total / weeks;
            if (milesPerWeek > 0)
                projected = DateTime.Today.AddDays(remain / milesPerWeek * 7);
        }

        return new ShoeStats
        {
            ShoeId              = shoe.Id,
            TotalMiles          = total,
            RunCount            = count,
            AvgDistance         = avg,
            LongestRun          = longest,
            LastRunDate         = lastRun,
            WornPercent         = worn,
            RemainingMiles      = remain,
            AvgPacePerMile      = avgPace,
            ProjectedRetirement = projected,
        };
    }

    public SummaryStats GetSummaryStats()
    {
        var now       = DateTime.Today;
        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var monthStart= new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        return new SummaryStats
        {
            WeekMiles  = Runs.Where(r => r.Date >= weekStart).Sum(r => r.Distance),
            WeekRuns   = Runs.Count(r => r.Date >= weekStart),
            MonthMiles = Runs.Where(r => r.Date >= monthStart).Sum(r => r.Distance),
            MonthRuns  = Runs.Count(r => r.Date >= monthStart),
            YearMiles  = Runs.Where(r => r.Date >= yearStart).Sum(r => r.Distance),
        };
    }
}
