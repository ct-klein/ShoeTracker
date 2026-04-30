using System.Text.Json;

namespace ShoeTracker.Services;

public sealed class SettingsService
{
    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public string Unit { get; set; } = "mi";

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
            if (doc.RootElement.TryGetProperty("unit", out var prop))
                Unit = prop.GetString() ?? "mi";
        }
        catch { /* first run */ }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(new { unit = Unit }));
        }
        catch { /* ignore */ }
    }
}
