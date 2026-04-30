# ShoeTracker

A Windows desktop app for tracking running shoe mileage. Log every run against a specific pair of shoes and get stats on wear, pace, and projected retirement date.

## Features

**Dashboard**
- Weekly, monthly, and yearly mileage summaries
- Per-shoe progress cards with a visual worn % bar

**Shoes**
- Add, edit, retire, and restore shoes
- Set a max mileage limit per shoe (default 400 mi)
- Click any shoe to see full stats: total miles, worn %, remaining miles, run count, average distance, longest run, average pace, last run date, and projected retirement date

**Log Run**
- Log a run with date, shoe, distance, duration, run type, surface, and notes
- Run types: Easy, Long, Tempo, Intervals, Race, Recovery, Cross Train
- Surfaces: Road, Trail, Track, Treadmill, Mixed

**History**
- Full run history with per-shoe filtering
- Delete individual entries

**Settings**
- Toggle between miles and kilometers

## Requirements

- Windows 10 or later
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## Run from source

```bash
cd ShoeTracker
dotnet build
dotnet run
```

Or run the pre-built executable directly:

```
ShoeTracker/bin/Debug/net9.0-windows/ShoeTracker.exe
```

## Data

All data is stored locally as JSON files next to the executable:

| File | Contents |
|------|----------|
| `data/shoes.json` | Shoe records |
| `data/runs.json` | Run log entries |
| `settings.json` | Unit preference (mi / km) |
| `Logs/yyyy-MM-dd.log` | Daily application log |

## Tech

- .NET 9.0 · Windows Forms
- No external NuGet packages — standard library only
- `System.Text.Json` for persistence
