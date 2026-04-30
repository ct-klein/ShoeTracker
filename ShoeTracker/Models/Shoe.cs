using System.Text.Json.Serialization;

namespace ShoeTracker.Models;

public sealed class Shoe
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("brand")]
    public string Brand { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("purchase_date")]
    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    [JsonPropertyName("max_mileage")]
    public double MaxMileage { get; set; } = 400.0;

    [JsonPropertyName("is_retired")]
    public bool IsRetired { get; set; } = false;

    [JsonPropertyName("retired_date")]
    public DateTime? RetiredDate { get; set; }

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
}
