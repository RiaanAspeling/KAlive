using System.Drawing;
using System.Drawing.Drawing2D;

namespace KAlive;

internal static class IconFactory
{
    public static Icon Create(MonitorState state)
    {
        Color fill = state switch
        {
            MonitorState.Paused => Color.FromArgb(140, 140, 140),
            MonitorState.Watching => Color.FromArgb(40, 180, 90),
            MonitorState.KeepingAwake => Color.FromArgb(240, 170, 30),
            _ => Color.Gray
        };

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(fill);
            g.FillEllipse(brush, 1, 1, 30, 30);

            // simple lightning bolt
            var bolt = new[]
            {
                new Point(18, 5),
                new Point(10, 18),
                new Point(15, 18),
                new Point(13, 27),
                new Point(22, 14),
                new Point(17, 14),
                new Point(20, 5)
            };
            using var boltBrush = new SolidBrush(Color.White);
            g.FillPolygon(boltBrush, bolt);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            Native.DestroyIcon(hIcon);
        }
    }
}
