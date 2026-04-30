using ShoeTracker.Controls;
using ShoeTracker.Models;
using ShoeTracker.Services;

namespace ShoeTracker;

public partial class MainForm : Form
{
    // ── Services ─────────────────────────────────────────────────────────────
    private static readonly LogService Log = LogService.Instance;
    private readonly DataService     _data     = new();
    private readonly SettingsService _settings = new();

    // ── Timer ────────────────────────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _ticker = new() { Interval = 1_000 };
    private DateTime _startTime = DateTime.Now;

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly List<NavButton>           _navBtns   = [];
    private readonly Dictionary<string, Panel> _tabPanels = [];

    // ── Drag / Resize ─────────────────────────────────────────────────────────
    private Point _dragAnchor;
    private bool  _dragging;

    // ── Dynamic refs: DASHBOARD ───────────────────────────────────────────────
    private Label          _lblWeekMiles  = null!;
    private Label          _lblWeekRuns   = null!;
    private Label          _lblMonthMiles = null!;
    private Label          _lblMonthRuns  = null!;
    private Label          _lblYearMiles  = null!;
    private FlowLayoutPanel _flpShoeCards = null!;

    // ── Dynamic refs: LOG RUN ─────────────────────────────────────────────────
    private ComboBox   _cmbLogShoe    = null!;
    private TextBox    _txtDistance   = null!;
    private TextBox    _txtDuration   = null!;
    private ComboBox   _cmbRunType    = null!;
    private ComboBox   _cmbSurface    = null!;
    private TextBox    _txtRunNotes   = null!;
    private DateTimePicker _dtpRunDate = null!;
    private FlowLayoutPanel _flpRecentRuns = null!;

    // ── Dynamic refs: HISTORY ─────────────────────────────────────────────────
    private ComboBox    _cmbHistoryShoe = null!;
    private FlowLayoutPanel _flpHistory = null!;
    private bool        _updatingHistory;

    // ── Status bar ────────────────────────────────────────────────────────────
    private Label _lblStatus = null!;

    // ─────────────────────────────────────────────────────────────────────────
    public MainForm()
    {
        InitializeComponent();
        _settings.Load();
        _data.Load();
        BuildUI();
        _ticker.Tick += (_, _) => UpdateStatus();
        _ticker.Start();
        RefreshAll();
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        SuspendLayout();

        // Title bar
        var titleBar = MakePanel(Color.FromArgb(8, 12, 18), DockStyle.Top, 40);
        AttachDrag(titleBar);

        var lblTitle = new Label
        {
            Text      = "◉  SHOE TRACKER",
            ForeColor = Theme.Accent,
            Font      = Theme.FontTitle,
            AutoSize  = false,
            Dock      = DockStyle.Left,
            Width     = 180,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(12, 0, 0, 0),
            BackColor = Color.Transparent,
        };

        var btnClose    = MakeTitleBtn("✕");
        var btnSettings = MakeTitleBtn("⚙");
        btnClose.Click    += (_, _) => Application.Exit();
        btnSettings.Click += (_, _) => ShowSettingsDialog();
        btnClose.Dock    = DockStyle.Right;
        btnSettings.Dock = DockStyle.Right;
        titleBar.Controls.AddRange([btnClose, btnSettings, lblTitle]);

        // Nav bar
        var pnlNav = MakePanel(Theme.Background, DockStyle.Top, 32);
        string[] tabs = ["DASHBOARD", "SHOES", "LOG RUN", "HISTORY"];
        var flpNav = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor     = Color.Transparent,
            Padding       = new Padding(4, 0, 0, 0),
        };
        foreach (var t in tabs)
        {
            var btn = new NavButton(t, t == "DASHBOARD");
            btn.Click += (_, _) => ShowTab(t);
            _navBtns.Add(btn);
            flpNav.Controls.Add(btn);
        }
        pnlNav.Controls.Add(flpNav);

        // Separator
        var sep = MakePanel(Theme.Border, DockStyle.Top, 1);

        // Status bar
        var statusBar = MakePanel(Color.FromArgb(8, 12, 18), DockStyle.Bottom, 22);
        _lblStatus = new Label
        {
            Text      = "ShoeTracker  v1.0",
            ForeColor = Theme.TextSecondary,
            Font      = Theme.FontSmall,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            BackColor = Color.Transparent,
        };
        statusBar.Controls.Add(_lblStatus);

        // Content
        var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        BuildAllTabs(pnlContent);

        // Resize grip
        var grip = new Panel { Size = new Size(14, 14), BackColor = Color.Transparent, Cursor = Cursors.SizeNWSE };
        grip.Paint     += DrawResizeGrip;
        grip.MouseDown += ResizeGrip_MouseDown;
        pnlContent.Controls.Add(grip);
        pnlContent.Resize += (_, _) =>
            grip.Location = new Point(pnlContent.ClientSize.Width - 14, pnlContent.ClientSize.Height - 14);

        Controls.AddRange([pnlContent, statusBar, sep, pnlNav, titleBar]);

        ShowTab("DASHBOARD");
        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildAllTabs(Panel container)
    {
        string[] tabs = ["DASHBOARD", "SHOES", "LOG RUN", "HISTORY"];
        foreach (var t in tabs)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, AutoScroll = true, Visible = false };
            _tabPanels[t] = p;
            container.Controls.Add(p);
        }

        BuildDashboardTab(_tabPanels["DASHBOARD"]);
        BuildShoesTab(_tabPanels["SHOES"]);
        BuildLogRunTab(_tabPanels["LOG RUN"]);
        BuildHistoryTab(_tabPanels["HISTORY"]);
    }

    private void ShowTab(string name)
    {
        foreach (var btn in _navBtns) btn.Active = btn.TabName == name;
        foreach (var kv in _tabPanels) kv.Value.Visible = kv.Key == name;

        if (name == "SHOES")    RefreshShoesTab();
        if (name == "HISTORY")  RefreshHistoryTab();
        if (name == "LOG RUN")  RefreshLogRunShoeList();
        if (name == "DASHBOARD") RefreshDashboard();
    }

    // ── Tab: DASHBOARD ────────────────────────────────────────────────────────

    private void BuildDashboardTab(Panel panel)
    {
        var wrap = MakeScrollWrap(panel);

        // Summary card
        wrap.Controls.Add(MakeSectionHeader("THIS WEEK"));
        var (weekCard, weekVals) = MakeCard(
            ("Miles",  "—", Theme.Accent),
            ("Runs",   "—", Theme.TextPrimary));
        _lblWeekMiles = weekVals[0];
        _lblWeekRuns  = weekVals[1];
        wrap.Controls.Add(AddMargin(weekCard, 8, 0, 8, 0));

        wrap.Controls.Add(MakeSectionHeader("THIS MONTH"));
        var (monthCard, monthVals) = MakeCard(
            ("Miles",  "—", Theme.Accent),
            ("Runs",   "—", Theme.TextPrimary));
        _lblMonthMiles = monthVals[0];
        _lblMonthRuns  = monthVals[1];
        wrap.Controls.Add(AddMargin(monthCard, 8, 0, 8, 0));

        wrap.Controls.Add(MakeSectionHeader("THIS YEAR"));
        var (yearCard, yearVals) = MakeCard(
            ("Miles",  "—", Theme.Accent));
        _lblYearMiles = yearVals[0];
        wrap.Controls.Add(AddMargin(yearCard, 8, 0, 8, 0));

        wrap.Controls.Add(MakeSectionHeader("ACTIVE SHOES"));
        _flpShoeCards = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 20),
        };
        wrap.Controls.Add(_flpShoeCards);
    }

    private void RefreshDashboard()
    {
        var summary = _data.GetSummaryStats();
        string unit = _settings.Unit;

        _lblWeekMiles.Text  = $"{summary.WeekMiles:F1} {unit}";
        _lblWeekRuns.Text   = summary.WeekRuns.ToString();
        _lblMonthMiles.Text = $"{summary.MonthMiles:F1} {unit}";
        _lblMonthRuns.Text  = summary.MonthRuns.ToString();
        _lblYearMiles.Text  = $"{summary.YearMiles:F1} {unit}";

        _flpShoeCards.SuspendLayout();
        _flpShoeCards.Controls.Clear();

        foreach (var shoe in _data.Shoes.Where(s => !s.IsRetired).OrderBy(s => s.Name))
        {
            var stats = _data.GetShoeStats(shoe);
            var card  = MakeShoeCard(shoe, stats, unit, _flpShoeCards.Width > 0 ? _flpShoeCards.Width : 404);
            _flpShoeCards.Controls.Add(card);
        }

        if (!_data.Shoes.Any(s => !s.IsRetired))
        {
            var hint = MakeBodyLabel("No active shoes. Go to SHOES tab to add your first pair.", 404);
            hint.Margin = new Padding(8, 4, 8, 0);
            _flpShoeCards.Controls.Add(hint);
        }

        _flpShoeCards.ResumeLayout(true);
    }

    private Panel MakeShoeCard(Shoe shoe, ShoeStats stats, string unit, int width)
    {
        int cardW = width - 16;
        bool nearEnd   = stats.WornPercent >= 80;
        bool veryNearEnd = stats.WornPercent >= 95;

        var card = new Panel
        {
            Width     = cardW,
            Height    = 108,
            BackColor = Theme.Surface,
            Margin    = new Padding(8, 0, 8, 6),
        };

        // Shoe name
        var lblName = new Label
        {
            Text      = shoe.Name.Length > 0 ? shoe.Name : "(unnamed)",
            Font      = Theme.FontMonoBold,
            ForeColor = veryNearEnd ? Theme.Negative : nearEnd ? Theme.Warning : Theme.Accent,
            Location  = new Point(10, 10),
            Size      = new Size(cardW - 20, 16),
            BackColor = Color.Transparent,
        };

        // Brand / Model
        string sub = (shoe.Brand + (shoe.Model.Length > 0 ? $"  {shoe.Model}" : "")).Trim();
        var lblSub = new Label
        {
            Text      = sub.Length > 0 ? sub : " ",
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location  = new Point(10, 28),
            Size      = new Size(cardW - 20, 14),
            BackColor = Color.Transparent,
        };

        // Mileage text
        string remaining = stats.RemainingMiles > 0
            ? $"{stats.TotalMiles:F1} / {shoe.MaxMileage:F0} {unit}  ·  {stats.RemainingMiles:F0} remaining"
            : $"{stats.TotalMiles:F1} {unit}  ·  LIMIT REACHED";
        var lblMiles = new Label
        {
            Text      = remaining,
            Font      = Theme.FontSmall,
            ForeColor = veryNearEnd ? Theme.Negative : nearEnd ? Theme.Warning : Theme.TextPrimary,
            Location  = new Point(10, 44),
            Size      = new Size(cardW - 20, 14),
            BackColor = Color.Transparent,
        };

        // Last run
        string lastRunStr = stats.LastRunDate.HasValue
            ? $"Last run: {stats.LastRunDate.Value:MMM d}  ·  {stats.RunCount} runs  ·  avg {stats.AvgDistance:F1} {unit}"
            : "No runs logged yet";
        var lblLast = new Label
        {
            Text      = lastRunStr,
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location  = new Point(10, 60),
            Size      = new Size(cardW - 20, 14),
            BackColor = Color.Transparent,
        };

        // Projected retirement
        string projStr = stats.ProjectedRetirement.HasValue
            ? $"Projected retirement: {stats.ProjectedRetirement.Value:MMM d, yyyy}"
            : "";
        var lblProj = new Label
        {
            Text      = projStr,
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location  = new Point(10, 74),
            Size      = new Size(cardW - 20, 14),
            BackColor = Color.Transparent,
        };

        // Progress bar background
        var barBg = new Panel
        {
            Location  = new Point(10, 91),
            Size      = new Size(cardW - 20, 6),
            BackColor = Theme.Border,
        };

        // Progress bar fill (drawn panel)
        int fillW = (int)Math.Min(cardW - 20, (cardW - 20) * stats.WornPercent / 100.0);
        var barFill = new Panel
        {
            Location  = new Point(0, 0),
            Size      = new Size(Math.Max(0, fillW), 6),
            BackColor = veryNearEnd ? Theme.Negative : nearEnd ? Theme.Warning : Theme.Positive,
        };
        barBg.Controls.Add(barFill);

        card.Controls.AddRange([lblName, lblSub, lblMiles, lblLast, lblProj, barBg]);

        card.Cursor = Cursors.Hand;
        card.Click += (_, _) => ShowShoeDetail(shoe);
        foreach (Control c in card.Controls) c.Click += (_, _) => ShowShoeDetail(shoe);

        return card;
    }

    // ── Tab: SHOES ────────────────────────────────────────────────────────────

    private void BuildShoesTab(Panel panel)
    {
        var wrap = MakeScrollWrap(panel);

        var btnAdd = new Button
        {
            Text      = "+ ADD SHOE",
            Font      = Theme.FontValue,
            ForeColor = Theme.Accent,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Height    = 28,
            Width     = 110,
            Margin    = new Padding(8, 8, 8, 4),
            TabStop   = false,
        };
        btnAdd.FlatAppearance.BorderColor = Theme.Accent;
        btnAdd.FlatAppearance.BorderSize  = 1;
        btnAdd.Click += (_, _) => { ShowAddShoeDialog(null); RefreshShoesTab(); RefreshDashboard(); };
        wrap.Controls.Add(btnAdd);

        var flp = new FlowLayoutPanel
        {
            Name          = "flpShoeList",
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 20),
        };
        wrap.Controls.Add(flp);
    }

    private void RefreshShoesTab()
    {
        var panel = _tabPanels["SHOES"];
        FlowLayoutPanel? flp = null;
        foreach (Control c in panel.Controls)
        {
            if (c is FlowLayoutPanel fp)
            {
                // find the scrollwrap, then find flpShoeList inside it
                foreach (Control inner in fp.Controls)
                    if (inner is FlowLayoutPanel named && named.Name == "flpShoeList")
                    { flp = named; break; }
            }
        }
        if (flp is null) return;

        flp.SuspendLayout();
        flp.Controls.Clear();

        string unit = _settings.Unit;
        var active  = _data.Shoes.Where(s => !s.IsRetired).OrderBy(s => s.Name).ToList();
        var retired = _data.Shoes.Where(s =>  s.IsRetired).OrderByDescending(s => s.RetiredDate).ToList();

        if (active.Count > 0)
        {
            flp.Controls.Add(MakeSectionHeader("ACTIVE"));
            foreach (var shoe in active) flp.Controls.Add(MakeShoeRow(shoe, unit, flp.Width));
        }
        if (retired.Count > 0)
        {
            flp.Controls.Add(MakeSectionHeader("RETIRED"));
            foreach (var shoe in retired) flp.Controls.Add(MakeShoeRow(shoe, unit, flp.Width));
        }
        if (_data.Shoes.Count == 0)
        {
            var hint = MakeBodyLabel("No shoes yet. Click + ADD SHOE above.", 404);
            hint.Margin = new Padding(8, 4, 8, 0);
            flp.Controls.Add(hint);
        }

        flp.ResumeLayout(true);
    }

    private Panel MakeShoeRow(Shoe shoe, string unit, int parentWidth)
    {
        int rowW  = Math.Max(200, parentWidth - 16);
        var stats = _data.GetShoeStats(shoe);

        var row = new Panel
        {
            Width     = rowW,
            Height    = 48,
            BackColor = Theme.Surface,
            Margin    = new Padding(8, 0, 8, 4),
        };

        var lblName = new Label
        {
            Text      = shoe.Name.Length > 0 ? shoe.Name : "(unnamed)",
            Font      = Theme.FontMonoBold,
            ForeColor = shoe.IsRetired ? Theme.TextSecondary : Theme.TextPrimary,
            Location  = new Point(10, 8),
            Size      = new Size(rowW - 160, 16),
            BackColor = Color.Transparent,
        };
        var lblMiles = new Label
        {
            Text      = $"{stats.TotalMiles:F1} / {shoe.MaxMileage:F0} {unit}  ({stats.WornPercent:F0}%)",
            Font      = Theme.FontSmall,
            ForeColor = stats.WornPercent >= 80 ? Theme.Warning : Theme.TextSecondary,
            Location  = new Point(10, 26),
            Size      = new Size(rowW - 160, 14),
            BackColor = Color.Transparent,
        };

        var btnEdit = new Button
        {
            Text      = "EDIT",
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(rowW - 140, 12),
            Size      = new Size(44, 22),
            TabStop   = false,
        };
        btnEdit.FlatAppearance.BorderSize  = 1;
        btnEdit.FlatAppearance.BorderColor = Theme.Border;
        btnEdit.Click += (_, _) => { ShowAddShoeDialog(shoe); RefreshShoesTab(); RefreshDashboard(); };

        var btnRetire = new Button
        {
            Text      = shoe.IsRetired ? "RESTORE" : "RETIRE",
            Font      = Theme.FontSmall,
            ForeColor = shoe.IsRetired ? Theme.Positive : Theme.Warning,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(rowW - 90, 12),
            Size      = new Size(54, 22),
            TabStop   = false,
        };
        btnRetire.FlatAppearance.BorderSize  = 1;
        btnRetire.FlatAppearance.BorderColor = shoe.IsRetired ? Theme.Positive : Theme.Warning;
        btnRetire.Click += (_, _) =>
        {
            shoe.IsRetired = !shoe.IsRetired;
            shoe.RetiredDate = shoe.IsRetired ? DateTime.Today : null;
            _data.UpdateShoe(shoe);
            RefreshShoesTab();
            RefreshDashboard();
        };

        row.Controls.AddRange([lblName, lblMiles, btnEdit, btnRetire]);
        row.Cursor = Cursors.Hand;
        row.Click += (_, _) => ShowShoeDetail(shoe);
        lblName.Click  += (_, _) => ShowShoeDetail(shoe);
        lblMiles.Click += (_, _) => ShowShoeDetail(shoe);

        return row;
    }

    // ── Tab: LOG RUN ──────────────────────────────────────────────────────────

    private void BuildLogRunTab(Panel panel)
    {
        var wrap = MakeScrollWrap(panel);
        wrap.Controls.Add(MakeSectionHeader("LOG A RUN"));

        var formCard = new Panel
        {
            Width     = 404,
            Height    = 320,
            BackColor = Theme.Surface,
            Margin    = new Padding(8, 0, 8, 12),
            Padding   = new Padding(10),
        };

        int y = 10;

        // Date + Shoe (row)
        formCard.Controls.Add(MakeFormLabel("Date", 10, y));
        formCard.Controls.Add(MakeFormLabel("Shoe", 210, y));
        _dtpRunDate = new DateTimePicker
        {
            Location  = new Point(10, y + 16),
            Size      = new Size(190, 22),
            Font      = Theme.FontLabel,
            Value     = DateTime.Today,
            Format    = DateTimePickerFormat.Short,
        };
        _cmbLogShoe = new ComboBox
        {
            Location      = new Point(210, y + 16),
            Size          = new Size(184, 22),
            Font          = Theme.FontLabel,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = Theme.SurfaceHover,
            ForeColor     = Theme.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        formCard.Controls.AddRange([_dtpRunDate, _cmbLogShoe]);
        y += 44;

        // Distance + Duration
        formCard.Controls.Add(MakeFormLabel($"Distance ({_settings.Unit})", 10, y));
        formCard.Controls.Add(MakeFormLabel("Duration (min, optional)", 210, y));
        _txtDistance = MakeFormInput(10, y + 16, 190, "");
        _txtDuration = MakeFormInput(210, y + 16, 184, "");
        _txtDistance.PlaceholderText = "e.g. 5.2";
        _txtDuration.PlaceholderText = "e.g. 28";
        formCard.Controls.AddRange([_txtDistance, _txtDuration]);
        y += 44;

        // Run Type + Surface
        formCard.Controls.Add(MakeFormLabel("Run Type", 10, y));
        formCard.Controls.Add(MakeFormLabel("Surface", 210, y));
        _cmbRunType = new ComboBox
        {
            Location      = new Point(10, y + 16),
            Size          = new Size(190, 22),
            Font          = Theme.FontLabel,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = Theme.SurfaceHover,
            ForeColor     = Theme.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cmbSurface = new ComboBox
        {
            Location      = new Point(210, y + 16),
            Size          = new Size(184, 22),
            Font          = Theme.FontLabel,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = Theme.SurfaceHover,
            ForeColor     = Theme.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        foreach (RunType rt in Enum.GetValues<RunType>()) _cmbRunType.Items.Add(rt.ToString());
        foreach (Surface s  in Enum.GetValues<Surface>()) _cmbSurface.Items.Add(s.ToString());
        _cmbRunType.SelectedIndex = 0;
        _cmbSurface.SelectedIndex = 0;
        formCard.Controls.AddRange([_cmbRunType, _cmbSurface]);
        y += 44;

        // Notes
        formCard.Controls.Add(MakeFormLabel("Notes (optional)", 10, y));
        _txtRunNotes = MakeFormInput(10, y + 16, 384, "");
        _txtRunNotes.PlaceholderText = "e.g. hilly 10k, felt strong";
        formCard.Controls.Add(_txtRunNotes);
        y += 44;

        // Submit
        var btnLog = new Button
        {
            Text      = "LOG RUN",
            Font      = Theme.FontValue,
            BackColor = Theme.AccentDim,
            ForeColor = Theme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(10, y),
            Size      = new Size(384, 30),
            TabStop   = false,
        };
        btnLog.FlatAppearance.BorderSize = 0;
        btnLog.Click += LogRun_Click;
        formCard.Controls.Add(btnLog);

        formCard.Height = y + 48;
        wrap.Controls.Add(formCard);

        wrap.Controls.Add(MakeSectionHeader("RECENT RUNS"));
        _flpRecentRuns = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 20),
        };
        wrap.Controls.Add(_flpRecentRuns);
    }

    private void RefreshLogRunShoeList()
    {
        if (_cmbLogShoe is null) return;
        var prev = _cmbLogShoe.SelectedItem?.ToString();
        _cmbLogShoe.Items.Clear();
        foreach (var shoe in _data.Shoes.Where(s => !s.IsRetired).OrderBy(s => s.Name))
            _cmbLogShoe.Items.Add(shoe.Name);
        if (_cmbLogShoe.Items.Count > 0)
            _cmbLogShoe.SelectedIndex = Math.Max(0,
                prev != null ? _cmbLogShoe.Items.IndexOf(prev) : 0);

        RefreshRecentRuns();
    }

    private void RefreshRecentRuns()
    {
        if (_flpRecentRuns is null) return;
        _flpRecentRuns.SuspendLayout();
        _flpRecentRuns.Controls.Clear();

        var recent = _data.Runs.OrderByDescending(r => r.Date).Take(10).ToList();
        string unit = _settings.Unit;

        foreach (var run in recent)
        {
            var shoe = _data.Shoes.FirstOrDefault(s => s.Id == run.ShoeId);
            string shoeName = shoe?.Name ?? "Unknown shoe";
            string pace = run.DurationMinutes.HasValue && run.Distance > 0
                ? $"  ·  {run.DurationMinutes.Value / run.Distance:F1} min/{unit}"
                : "";
            string line = $"{run.Date:MMM d}  ·  {shoeName}  ·  {run.Distance:F1} {unit}  ·  {run.RunType}{pace}";

            var row = new Panel
            {
                Width     = _flpRecentRuns.Width > 0 ? _flpRecentRuns.Width - 16 : 388,
                Height    = 32,
                BackColor = Theme.Surface,
                Margin    = new Padding(8, 0, 8, 3),
            };
            var lbl = new Label
            {
                Text      = line,
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextPrimary,
                Location  = new Point(10, 0),
                Size      = new Size(row.Width - 50, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            var btnDel = new Button
            {
                Text      = "✕",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(row.Width - 30, 6),
                Size      = new Size(22, 20),
                TabStop   = false,
            };
            btnDel.FlatAppearance.BorderSize = 0;
            var runId = run.Id;
            btnDel.Click += (_, _) =>
            {
                _data.DeleteRun(runId);
                RefreshRecentRuns();
                RefreshDashboard();
                RefreshHistoryTab();
            };
            row.Controls.AddRange([lbl, btnDel]);
            _flpRecentRuns.Controls.Add(row);
        }

        if (recent.Count == 0)
        {
            var hint = MakeBodyLabel("No runs logged yet.", 404);
            hint.Margin = new Padding(8, 4, 8, 0);
            _flpRecentRuns.Controls.Add(hint);
        }

        _flpRecentRuns.ResumeLayout(true);
    }

    private void LogRun_Click(object? sender, EventArgs e)
    {
        if (_cmbLogShoe.SelectedIndex < 0)
        {
            MessageBox.Show("Please select a shoe first.", "No Shoe Selected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!double.TryParse(_txtDistance.Text.Trim(), out double dist) || dist <= 0)
        {
            MessageBox.Show("Please enter a valid distance.", "Invalid Distance",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int? duration = null;
        if (!string.IsNullOrWhiteSpace(_txtDuration.Text))
        {
            if (!int.TryParse(_txtDuration.Text.Trim(), out int dur) || dur <= 0)
            {
                MessageBox.Show("Duration must be a whole number of minutes, or leave blank.",
                    "Invalid Duration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            duration = dur;
        }

        var shoeName = _cmbLogShoe.SelectedItem!.ToString()!;
        var shoe     = _data.Shoes.First(s => s.Name == shoeName && !s.IsRetired);

        var run = new RunEntry
        {
            ShoeId          = shoe.Id,
            Date            = _dtpRunDate.Value.Date,
            Distance        = dist,
            DurationMinutes = duration,
            RunType         = Enum.Parse<RunType>(_cmbRunType.SelectedItem!.ToString()!),
            Surface         = Enum.Parse<Surface>(_cmbSurface.SelectedItem!.ToString()!),
            Notes           = _txtRunNotes.Text.Trim(),
        };

        _data.AddRun(run);
        Log.Info($"Logged run: {dist} {_settings.Unit} on '{shoe.Name}'");

        _txtDistance.Text  = "";
        _txtDuration.Text  = "";
        _txtRunNotes.Text  = "";
        _dtpRunDate.Value  = DateTime.Today;
        _cmbRunType.SelectedIndex = 0;
        _cmbSurface.SelectedIndex = 0;

        RefreshRecentRuns();
        RefreshDashboard();
        RefreshHistoryTab();

        SetStatus($"Logged {dist:F1} {_settings.Unit} on {shoe.Name}");
    }

    // ── Tab: HISTORY ──────────────────────────────────────────────────────────

    private void BuildHistoryTab(Panel panel)
    {
        var wrap = MakeScrollWrap(panel);

        // Filter row
        var filterRow = new Panel
        {
            Width     = 404,
            Height    = 38,
            BackColor = Color.Transparent,
            Margin    = new Padding(8, 8, 8, 0),
        };
        var lblFilter = new Label
        {
            Text      = "Filter by shoe:",
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location  = new Point(0, 10),
            AutoSize  = true,
            BackColor = Color.Transparent,
        };
        _cmbHistoryShoe = new ComboBox
        {
            Location      = new Point(90, 6),
            Size          = new Size(220, 22),
            Font          = Theme.FontLabel,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = Theme.SurfaceHover,
            ForeColor     = Theme.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cmbHistoryShoe.SelectedIndexChanged += (_, _) => RefreshHistoryTab();
        filterRow.Controls.AddRange([lblFilter, _cmbHistoryShoe]);
        wrap.Controls.Add(filterRow);

        _flpHistory = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 20),
        };
        wrap.Controls.Add(_flpHistory);
    }

    private void RefreshHistoryTab()
    {
        if (_flpHistory is null || _cmbHistoryShoe is null) return;
        if (_updatingHistory) return;
        _updatingHistory = true;
        try
        {
        // Rebuild shoe filter list
        var prevShoe = _cmbHistoryShoe.SelectedItem?.ToString();
        _cmbHistoryShoe.Items.Clear();
        _cmbHistoryShoe.Items.Add("All Shoes");
        foreach (var s in _data.Shoes.OrderBy(s => s.Name))
            _cmbHistoryShoe.Items.Add(s.Name);

        int selIdx = 0;
        if (prevShoe != null)
        {
            int idx = _cmbHistoryShoe.Items.IndexOf(prevShoe);
            if (idx >= 0) selIdx = idx;
        }
        _cmbHistoryShoe.SelectedIndex = selIdx;

        string unit    = _settings.Unit;
        string filter  = _cmbHistoryShoe.SelectedItem?.ToString() ?? "All Shoes";
        Guid?  shoeId  = filter == "All Shoes" ? null
            : _data.Shoes.FirstOrDefault(s => s.Name == filter)?.Id;

        var runs = _data.Runs
            .Where(r => shoeId == null || r.ShoeId == shoeId)
            .OrderByDescending(r => r.Date)
            .ToList();

        _flpHistory.SuspendLayout();
        _flpHistory.Controls.Clear();

        if (runs.Count == 0)
        {
            var hint = MakeBodyLabel("No runs found.", 404);
            hint.Margin = new Padding(8, 4, 8, 0);
            _flpHistory.Controls.Add(hint);
            _flpHistory.ResumeLayout(true);
            return;
        } // end "no runs" early-return block

        // Header row
        var hdr = new Panel
        {
            Width     = Math.Max(200, _flpHistory.Width - 16),
            Height    = 22,
            BackColor = Color.Transparent,
            Margin    = new Padding(8, 4, 8, 2),
        };
        void AddHdrLbl(string text, int x, int w) => hdr.Controls.Add(new Label
        {
            Text      = text,
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location  = new Point(x, 2),
            Size      = new Size(w, 18),
            BackColor = Color.Transparent,
        });
        AddHdrLbl("DATE",     0,   60);
        AddHdrLbl("SHOE",     62,  120);
        AddHdrLbl("DIST",     184, 50);
        AddHdrLbl("TYPE",     236, 65);
        AddHdrLbl("PACE",     303, 70);
        _flpHistory.Controls.Add(hdr);

        foreach (var run in runs)
        {
            var shoe = _data.Shoes.FirstOrDefault(s => s.Id == run.ShoeId);
            string shoeName = shoe?.Name ?? "?";
            string pace = run.DurationMinutes.HasValue && run.Distance > 0
                ? $"{run.DurationMinutes.Value / run.Distance:F1}m/{unit}"
                : "—";

            int rowW = Math.Max(200, _flpHistory.Width - 16);
            var row = new Panel
            {
                Width     = rowW,
                Height    = run.Notes.Length > 0 ? 42 : 28,
                BackColor = Theme.Surface,
                Margin    = new Padding(8, 0, 8, 2),
            };
            void AddCell(string text, int x, int w, Color color) => row.Controls.Add(new Label
            {
                Text      = text,
                Font      = Theme.FontSmall,
                ForeColor = color,
                Location  = new Point(x, 6),
                Size      = new Size(w, 16),
                BackColor = Color.Transparent,
            });
            AddCell(run.Date.ToString("MMM d"),  0,   60,  Theme.TextPrimary);
            AddCell(shoeName,                    62,  118, Theme.Accent);
            AddCell($"{run.Distance:F1}",        184, 50,  Theme.TextPrimary);
            AddCell(run.RunType.ToString(),      236, 65,  Theme.TextSecondary);
            AddCell(pace,                        303, 70,  Theme.TextSecondary);

            if (run.Notes.Length > 0)
            {
                row.Controls.Add(new Label
                {
                    Text      = run.Notes,
                    Font      = Theme.FontSmall,
                    ForeColor = Theme.TextSecondary,
                    Location  = new Point(62, 22),
                    Size      = new Size(rowW - 100, 14),
                    BackColor = Color.Transparent,
                });
            }

            var btnDel = new Button
            {
                Text      = "✕",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(rowW - 24, 4),
                Size      = new Size(20, 20),
                TabStop   = false,
            };
            btnDel.FlatAppearance.BorderSize = 0;
            var runId = run.Id;
            btnDel.Click += (_, _) =>
            {
                _data.DeleteRun(runId);
                RefreshHistoryTab();
                RefreshDashboard();
                RefreshRecentRuns();
            };
            row.Controls.Add(btnDel);
            _flpHistory.Controls.Add(row);
        }

        _flpHistory.ResumeLayout(true);
        } // end try
        finally { _updatingHistory = false; }
    }

    // ── Dialogs ───────────────────────────────────────────────────────────────

    private void ShowAddShoeDialog(Shoe? existing)
    {
        bool isEdit = existing != null;
        using var dlg = new Form
        {
            Text            = isEdit ? "Edit Shoe" : "Add Shoe",
            ClientSize      = new Size(360, 310),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition   = FormStartPosition.CenterParent,
            BackColor       = Theme.Surface,
            MaximizeBox     = false,
            MinimizeBox     = false,
        };

        int y = 14;
        void AddLbl(string text, int x)
        {
            dlg.Controls.Add(new Label
            {
                Text      = text,
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Location  = new Point(x, y),
                AutoSize  = true,
                BackColor = Color.Transparent,
            });
        }
        TextBox MakeTxt(int x, int w, string val) => new()
        {
            Text        = val,
            Font        = Theme.FontLabel,
            BackColor   = Theme.Background,
            ForeColor   = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Location    = new Point(x, y + 14),
            Size        = new Size(w, 22),
        };

        // Name
        AddLbl("Shoe Name *", 12);
        var txtName = MakeTxt(12, 336, existing?.Name ?? "");
        dlg.Controls.Add(txtName);
        y += 42;

        // Brand + Model
        AddLbl("Brand", 12);
        AddLbl("Model", 190);
        var txtBrand = MakeTxt(12, 170, existing?.Brand ?? "");
        var txtModel = MakeTxt(190, 158, existing?.Model ?? "");
        dlg.Controls.AddRange([txtBrand, txtModel]);
        y += 42;

        // Max Mileage + Purchase Date
        AddLbl($"Max Mileage ({_settings.Unit})", 12);
        AddLbl("Purchase Date", 190);
        var txtMaxMiles = MakeTxt(12, 170, existing?.MaxMileage.ToString("F0") ?? "400");
        var dtpPurchase = new DateTimePicker
        {
            Location = new Point(190, y + 14),
            Size     = new Size(158, 22),
            Font     = Theme.FontLabel,
            Value    = existing?.PurchaseDate ?? DateTime.Today,
            Format   = DateTimePickerFormat.Short,
        };
        dlg.Controls.AddRange([txtMaxMiles, dtpPurchase]);
        y += 42;

        // Notes
        AddLbl("Notes (optional)", 12);
        var txtNotes = MakeTxt(12, 336, existing?.Notes ?? "");
        dlg.Controls.Add(txtNotes);
        y += 42;

        // Buttons
        var btnSave = new Button
        {
            Text         = isEdit ? "Save Changes" : "Add Shoe",
            Font         = Theme.FontValue,
            BackColor    = Theme.AccentDim,
            ForeColor    = Theme.TextPrimary,
            FlatStyle    = FlatStyle.Flat,
            Location     = new Point(12, y + 10),
            Size         = new Size(160, 28),
            DialogResult = DialogResult.OK,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        dlg.Controls.Add(btnSave);
        dlg.AcceptButton = btnSave;
        dlg.ClientSize   = new Size(360, y + 56);

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Shoe name is required.", "Missing Name",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!double.TryParse(txtMaxMiles.Text, out double maxMiles) || maxMiles <= 0)
            maxMiles = 400.0;

        if (isEdit)
        {
            existing!.Name         = name;
            existing.Brand         = txtBrand.Text.Trim();
            existing.Model         = txtModel.Text.Trim();
            existing.MaxMileage    = maxMiles;
            existing.PurchaseDate  = dtpPurchase.Value.Date;
            existing.Notes         = txtNotes.Text.Trim();
            _data.UpdateShoe(existing);
        }
        else
        {
            _data.AddShoe(new Shoe
            {
                Name         = name,
                Brand        = txtBrand.Text.Trim(),
                Model        = txtModel.Text.Trim(),
                MaxMileage   = maxMiles,
                PurchaseDate = dtpPurchase.Value.Date,
                Notes        = txtNotes.Text.Trim(),
            });
        }
    }

    private void ShowShoeDetail(Shoe shoe)
    {
        var stats = _data.GetShoeStats(shoe);
        string unit = _settings.Unit;

        string avgPaceStr = stats.AvgPacePerMile.HasValue
            ? $"{(int)stats.AvgPacePerMile.Value}:{(int)((stats.AvgPacePerMile.Value % 1) * 60):D2} min/{unit}"
            : "N/A";
        string projStr = stats.ProjectedRetirement.HasValue
            ? stats.ProjectedRetirement.Value.ToString("MMM d, yyyy")
            : "N/A";

        string msg =
            $"Shoe:        {shoe.Name}\n" +
            $"Brand/Model: {shoe.Brand} {shoe.Model}\n" +
            $"Purchased:   {shoe.PurchaseDate:MMM d, yyyy}\n" +
            $"Status:      {(shoe.IsRetired ? "Retired" : "Active")}\n\n" +
            $"Total Miles: {stats.TotalMiles:F1} {unit}\n" +
            $"Max Mileage: {shoe.MaxMileage:F0} {unit}\n" +
            $"Worn:        {stats.WornPercent:F1}%\n" +
            $"Remaining:   {stats.RemainingMiles:F0} {unit}\n\n" +
            $"Runs:        {stats.RunCount}\n" +
            $"Avg Distance:{stats.AvgDistance:F1} {unit}\n" +
            $"Longest Run: {stats.LongestRun:F1} {unit}\n" +
            $"Avg Pace:    {avgPaceStr}\n" +
            $"Last Run:    {(stats.LastRunDate.HasValue ? stats.LastRunDate.Value.ToString("MMM d, yyyy") : "Never")}\n" +
            $"Proj. Retire:{projStr}\n\n" +
            (shoe.Notes.Length > 0 ? $"Notes: {shoe.Notes}" : "");

        MessageBox.Show(msg, shoe.Name, MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    private void ShowSettingsDialog()
    {
        using var dlg = new Form
        {
            Text            = "Settings",
            ClientSize      = new Size(280, 110),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition   = FormStartPosition.CenterParent,
            BackColor       = Theme.Surface,
            MaximizeBox     = false,
            MinimizeBox     = false,
        };

        dlg.Controls.Add(new Label
        {
            Text      = "Distance Unit:",
            Font      = Theme.FontLabel,
            ForeColor = Theme.TextPrimary,
            Location  = new Point(12, 16),
            AutoSize  = true,
            BackColor = Color.Transparent,
        });

        var cmbUnit = new ComboBox
        {
            Location      = new Point(110, 12),
            Size          = new Size(80, 22),
            Font          = Theme.FontLabel,
            BackColor     = Theme.Background,
            ForeColor     = Theme.TextPrimary,
            FlatStyle     = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        cmbUnit.Items.AddRange(["mi", "km"]);
        cmbUnit.SelectedItem = _settings.Unit;
        dlg.Controls.Add(cmbUnit);

        var btnSave = new Button
        {
            Text         = "Save",
            Font         = Theme.FontValue,
            BackColor    = Theme.AccentDim,
            ForeColor    = Theme.TextPrimary,
            FlatStyle    = FlatStyle.Flat,
            Location     = new Point(12, 50),
            Size         = new Size(120, 28),
            DialogResult = DialogResult.OK,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        dlg.Controls.Add(btnSave);
        dlg.AcceptButton = btnSave;

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings.Unit = cmbUnit.SelectedItem?.ToString() ?? "mi";
            _settings.Save();
            RefreshAll();
        }
    }

    // ── Status / Helpers ──────────────────────────────────────────────────────

    private void RefreshAll()
    {
        RefreshDashboard();
        RefreshLogRunShoeList();
        RefreshHistoryTab();
    }

    private void SetStatus(string text) => _lblStatus.Text = text;

    private void UpdateStatus()
    {
        int active  = _data.Shoes.Count(s => !s.IsRetired);
        int runCount = _data.Runs.Count;
        _lblStatus.Text = $"{active} active shoes  ·  {runCount} runs logged  ·  ShoeTracker v1.0";
    }

    // ── Window Drag & Resize ──────────────────────────────────────────────────

    private void AttachDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging   = true;
            _dragAnchor = e.Location;
        };
        control.MouseMove += (_, e) =>
        {
            if (!_dragging) return;
            var screen = control.PointToScreen(e.Location);
            Location = new Point(screen.X - _dragAnchor.X, screen.Y - _dragAnchor.Y);
        };
        control.MouseUp += (_, _) => _dragging = false;
    }

    private void ResizeGrip_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        NativeMethods.SendResizeMessage(Handle);
    }

    private static void DrawResizeGrip(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Transparent);
        using var pen = new Pen(Theme.Border, 1);
        for (int i = 2; i < 12; i += 4)
            e.Graphics.DrawLine(pen, i, 14, 14, i);
    }

    // ── UI Helpers ────────────────────────────────────────────────────────────

    private static FlowLayoutPanel MakeScrollWrap(Panel parent)
    {
        var flp = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            Dock          = DockStyle.Top,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0),
        };
        parent.Controls.Add(flp);
        parent.SizeChanged += (_, _) =>
        {
            flp.Width = parent.ClientSize.Width;
            foreach (Control c in flp.Controls)
                if (c != null) c.Width = Math.Max(10, parent.ClientSize.Width - 16);
        };
        return flp;
    }

    private static (Panel card, Label[] vals) MakeCard(
        params (string label, string value, Color color)[] rows)
    {
        const int rowH = 22;
        const int padX = 10;
        const int padY = 8;
        int totalH = rows.Length * rowH + padY * 2;
        int cardW  = 404;

        var card = new Panel { Size = new Size(cardW, totalH), BackColor = Theme.Surface, Margin = new Padding(0) };
        var vals = new Label[rows.Length];

        for (int i = 0; i < rows.Length; i++)
        {
            int y = padY + i * rowH;
            var (labelText, valueText, color) = rows[i];
            card.Controls.Add(new Label
            {
                Text      = labelText,
                Font      = Theme.FontLabel,
                ForeColor = Theme.TextPrimary,
                Location  = new Point(padX, y),
                Size      = new Size(cardW * 55 / 100, rowH),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
            });
            var val = new Label
            {
                Text      = valueText,
                Font      = Theme.FontValue,
                ForeColor = color,
                Location  = new Point(padX + cardW * 55 / 100, y),
                Size      = new Size(cardW * 45 / 100 - padX * 2, rowH),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
            };
            vals[i] = val;
            card.Controls.Add(val);
        }

        return (card, vals);
    }

    private static Label MakeSectionHeader(string text) => new()
    {
        Text      = text,
        Font      = Theme.FontSection,
        ForeColor = Theme.TextSecondary,
        AutoSize  = false,
        Height    = 26,
        Width     = 420,
        TextAlign = ContentAlignment.BottomLeft,
        Padding   = new Padding(8, 0, 0, 2),
        BackColor = Color.Transparent,
        Margin    = new Padding(0, 8, 0, 0),
    };

    private static Label MakeBodyLabel(string text, int width = 404)
    {
        var size = TextRenderer.MeasureText(text, Theme.FontSmall,
            new Size(width - 16, 0), TextFormatFlags.WordBreak);
        return new Label
        {
            Text      = text,
            Font      = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            AutoSize  = false,
            Width     = width,
            Height    = size.Height + 4,
            BackColor = Color.Transparent,
            Padding   = new Padding(0),
        };
    }

    private static Panel MakePanel(Color back, DockStyle dock, int height) =>
        new() { BackColor = back, Dock = dock, Height = height };

    private static Button MakeTitleBtn(string text)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = Theme.FontNav,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(32, 40),
            TabStop   = false,
        };
        btn.FlatAppearance.BorderSize           = 0;
        btn.FlatAppearance.MouseOverBackColor   = Color.FromArgb(30, 255, 255, 255);
        btn.FlatAppearance.MouseDownBackColor   = Color.FromArgb(50, 255, 255, 255);
        return btn;
    }

    private static Panel AddMargin(Panel p, int left, int top, int right, int bottom)
    {
        p.Margin = new Padding(left, top, right, bottom);
        return p;
    }

    private static Label MakeFormLabel(string text, int x, int y) => new()
    {
        Text      = text,
        Font      = Theme.FontSmall,
        ForeColor = Theme.TextSecondary,
        Location  = new Point(x, y),
        AutoSize  = true,
        BackColor = Color.Transparent,
    };

    private static TextBox MakeFormInput(int x, int y, int width, string text) => new()
    {
        Text        = text,
        Font        = Theme.FontLabel,
        BackColor   = Theme.SurfaceHover,
        ForeColor   = Theme.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        Location    = new Point(x, y),
        Size        = new Size(width, 22),
    };
}

// ── Native helpers for resize grip ────────────────────────────────────────────

internal static class NativeMethods
{
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_BOTTOMRIGHT   = 17;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    public static void SendResizeMessage(IntPtr handle)
    {
        ReleaseCapture();
        SendMessage(handle, WM_NCLBUTTONDOWN, HT_BOTTOMRIGHT, 0);
    }
}
