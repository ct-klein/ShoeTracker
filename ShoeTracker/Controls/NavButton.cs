using System.ComponentModel;

namespace ShoeTracker.Controls;

public sealed class NavButton : Button
{
    private bool _active;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TabName { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Active
    {
        get => _active;
        set { _active = value; Invalidate(); }
    }

    public NavButton(string tabName, bool active = false)
    {
        TabName   = tabName;
        _active   = active;
        Text      = tabName;
        Font      = Theme.FontNav;
        AutoSize  = false;
        Height    = 32;
        Width     = TextRenderer.MeasureText(tabName, Font).Width + 24;
        FlatStyle = FlatStyle.Flat;
        BackColor = Color.Transparent;
        ForeColor = Theme.TextSecondary;
        Cursor    = Cursors.Hand;
        Margin    = new Padding(0);
        Padding   = new Padding(0);
        TabStop   = false;

        FlatAppearance.BorderSize           = 0;
        FlatAppearance.MouseOverBackColor   = Color.Transparent;
        FlatAppearance.MouseDownBackColor   = Color.Transparent;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Background);

        var textColor = _active ? Theme.Accent : Theme.TextSecondary;
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        if (_active)
        {
            using var pen = new Pen(Theme.Accent, 2);
            g.DrawLine(pen, 2, Height - 2, Width - 2, Height - 2);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        if (!_active) ForeColor = Theme.TextPrimary;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        ForeColor = _active ? Theme.Accent : Theme.TextSecondary;
        Invalidate();
        base.OnMouseLeave(e);
    }
}
