using System.Text.Json.Serialization;

namespace ShoeTracker.Models;

public enum RunType  { Easy, Long, Tempo, Intervals, Race, Recovery, CrossTrain }
public enum Surface  { Road, Trail, Track, Treadmill, Mixed }

public sealed class RunEntry
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("shoe_id")]
    public Guid ShoeId { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [JsonPropertyName("run_type")]
    public RunType RunType { get; set; } = RunType.Easy;

    [JsonPropertyName("surface")]
    public Surface Surface { get; set; } = Surface.Road;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
}

public sealed class ShoeStats
{
    public Guid      ShoeId              { get; init; }
    public double    TotalMiles          { get; init; }
    public int       RunCount            { get; init; }
    public double    AvgDistance         { get; init; }
    public double    LongestRun          { get; init; }
    public DateTime? LastRunDate         { get; init; }
    public double    WornPercent         { get; init; }
    public double    RemainingMiles      { get; init; }
    public double?   AvgPacePerMile      { get; init; }
    public DateTime? ProjectedRetirement { get; init; }
}

public sealed class SummaryStats
{
    public double WeekMiles  { get; init; }
    public int    WeekRuns   { get; init; }
    public double MonthMiles { get; init; }
    public int    MonthRuns  { get; init; }
    public double YearMiles  { get; init; }
}
