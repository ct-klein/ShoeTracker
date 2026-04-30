# ShoeTracker

## Overview
Windows Forms (.NET 9.0) application for tracking running shoe mileage. Logs runs per shoe and displays stats (total miles, % worn, average pace, projected retirement date).

## Build & Run
```bash
cd ShoeTracker
dotnet build
dotnet run
```

## Project Conventions
- **Framework**: .NET 9.0, Windows Forms, borderless window
- **Theme**: Dark theme via `Theme.cs` static class — Consolas font, teal accent `(0, 200, 255)`
- **No external NuGet packages** — only .NET framework libraries
- **JSON**: `System.Text.Json` with `[JsonPropertyName]` attributes for snake_case mapping
- **Classes**: Use `sealed` on classes not intended for inheritance
- **Logging**: `LogService.Instance` singleton — logs to `Logs/{yyyy-MM-dd}.log`, thread-safe
- **Data storage**: JSON files in `data/shoes.json` and `data/runs.json` (next to the exe)
- **Settings**: `settings.json` next to exe — stores distance unit preference (`mi` or `km`)
- **UI**: All UI built in code (no Designer layout). `BuildUI()` pattern from `MainForm.cs`
- **Error handling**: Catch and log exceptions at file I/O boundaries

## Architecture
- `Models/Shoe.cs` — Shoe entity + ShoeStats / SummaryStats computed view models
- `Models/RunEntry.cs` — RunEntry entity, RunType enum, Surface enum
- `Services/LogService.cs` — Thread-safe daily file logger singleton
- `Services/SettingsService.cs` — Persists unit preference to settings.json
- `Services/DataService.cs` — Loads/saves all data; computes shoe stats and summary stats
- `Controls/NavButton.cs` — Custom tab nav button with underline indicator
- `Theme.cs` — Colors and fonts constants
- `MainForm.cs` — Main form, all four tabs: DASHBOARD, SHOES, LOG RUN, HISTORY
- `MainForm.Designer.cs` — Minimal InitializeComponent (form properties only)

## Tabs
- **DASHBOARD** — Weekly/monthly/yearly mileage summary + per-shoe progress cards with worn % bar
- **SHOES** — Add, edit, retire/restore shoes; click any shoe row for full stats detail
- **LOG RUN** — Form to log a run (date, shoe, distance, duration, type, surface, notes)
- **HISTORY** — Full run history with per-shoe filter and delete capability

## Stats Tracked Per Shoe
- Total miles, worn %, remaining miles
- Run count, average distance, longest run
- Average pace (min/mile or min/km) when duration is logged
- Last run date, projected retirement date (based on rolling weekly avg)
