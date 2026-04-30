#nullable disable

namespace ShoeTracker;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _ticker?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode       = AutoScaleMode.Font;
        BackColor           = Color.FromArgb(13, 17, 23);
        ClientSize          = new Size(420, 720);
        FormBorderStyle     = FormBorderStyle.None;
        MinimumSize         = new Size(380, 540);
        Name                = "MainForm";
        Text                = "ShoeTracker";
        StartPosition       = FormStartPosition.CenterScreen;
        ResumeLayout(false);
    }
}
