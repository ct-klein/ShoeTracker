namespace ShoeTracker;

static class Theme
{
    // Mutable palette — call Apply() once before BuildUI()
    public static Color Background    = Color.Black;
    public static Color Surface       = Color.Black;
    public static Color SurfaceHover  = Color.Black;
    public static Color Border        = Color.Black;
    public static Color Accent        = Color.Black;
    public static Color AccentDim     = Color.Black;
    public static Color TextPrimary   = Color.Black;
    public static Color TextSecondary = Color.Black;
    public static Color TitleBar      = Color.Black;

    public static readonly Color Negative = Color.FromArgb(220, 60, 50);
    public static readonly Color Warning  = Color.FromArgb(210, 140, 0);
    public static readonly Color Positive = Color.FromArgb(50, 170, 70);

    public static Font FontTitle    = null!;
    public static Font FontNav      = null!;
    public static Font FontLabel    = null!;
    public static Font FontValue    = null!;
    public static Font FontLarge    = null!;
    public static Font FontSmall    = null!;
    public static Font FontMono     = null!;
    public static Font FontMonoBold = null!;
    public static Font FontSection  = null!;

    public static void Apply(bool light)
    {
        if (light)
        {
            Background    = Color.FromArgb(245, 247, 249);
            Surface       = Color.White;
            SurfaceHover  = Color.FromArgb(232, 236, 241);
            Border        = Color.FromArgb(200, 210, 220);
            Accent        = Color.FromArgb(0, 120, 212);
            AccentDim     = Color.FromArgb(0, 80, 160);
            TextPrimary   = Color.FromArgb(24, 24, 24);
            TextSecondary = Color.FromArgb(95, 108, 122);
            TitleBar      = Color.FromArgb(220, 227, 234);
        }
        else
        {
            Background    = Color.FromArgb(13, 17, 23);
            Surface       = Color.FromArgb(22, 27, 34);
            SurfaceHover  = Color.FromArgb(30, 37, 46);
            Border        = Color.FromArgb(48, 54, 61);
            Accent        = Color.FromArgb(0, 200, 255);
            AccentDim     = Color.FromArgb(0, 100, 160);
            TextPrimary   = Color.FromArgb(230, 237, 243);
            TextSecondary = Color.FromArgb(139, 148, 158);
            TitleBar      = Color.FromArgb(8, 12, 18);
        }

        FontTitle    = new Font("Consolas", 11f,  FontStyle.Bold);
        FontNav      = new Font("Consolas", 10f,  FontStyle.Regular);
        FontLabel    = new Font("Consolas", 10f,  FontStyle.Regular);
        FontValue    = new Font("Consolas", 10f,  FontStyle.Bold);
        FontLarge    = new Font("Consolas", 15f,  FontStyle.Bold);
        FontSmall    = new Font("Consolas", 9f,   FontStyle.Regular);
        FontMono     = new Font("Consolas", 9.5f, FontStyle.Regular);
        FontMonoBold = new Font("Consolas", 9.5f, FontStyle.Bold);
        FontSection  = new Font("Consolas", 9f,   FontStyle.Regular);
    }
}
