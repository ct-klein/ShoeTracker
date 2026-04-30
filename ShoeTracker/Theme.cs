namespace ShoeTracker;

static class Theme
{
    public static readonly Color Background    = Color.FromArgb(13, 17, 23);
    public static readonly Color Surface       = Color.FromArgb(22, 27, 34);
    public static readonly Color SurfaceHover  = Color.FromArgb(30, 37, 46);
    public static readonly Color Border        = Color.FromArgb(48, 54, 61);
    public static readonly Color Accent        = Color.FromArgb(0, 200, 255);
    public static readonly Color AccentDim     = Color.FromArgb(0, 100, 160);
    public static readonly Color TextPrimary   = Color.FromArgb(230, 237, 243);
    public static readonly Color TextSecondary = Color.FromArgb(139, 148, 158);
    public static readonly Color Negative      = Color.FromArgb(248, 81, 73);
    public static readonly Color Warning       = Color.FromArgb(255, 166, 0);
    public static readonly Color Positive      = Color.FromArgb(63, 185, 80);

    public static readonly Font FontTitle    = new("Consolas", 9f,  FontStyle.Bold);
    public static readonly Font FontNav      = new("Consolas", 8f,  FontStyle.Regular);
    public static readonly Font FontLabel    = new("Consolas", 8f,  FontStyle.Regular);
    public static readonly Font FontValue    = new("Consolas", 8f,  FontStyle.Bold);
    public static readonly Font FontLarge    = new("Consolas", 12f, FontStyle.Bold);
    public static readonly Font FontSmall    = new("Consolas", 7f,  FontStyle.Regular);
    public static readonly Font FontMono     = new("Consolas", 7.5f, FontStyle.Regular);
    public static readonly Font FontMonoBold = new("Consolas", 7.5f, FontStyle.Bold);
    public static readonly Font FontSection  = new("Consolas", 7f,  FontStyle.Regular);
}
