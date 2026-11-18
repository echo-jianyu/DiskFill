using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI
{
    internal static class AppTheme
    {
        public static readonly Color Canvas = Color.FromArgb(244, 246, 248);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceMuted = Color.FromArgb(248, 250, 251);
        public static readonly Color Border = Color.FromArgb(222, 227, 232);
        public static readonly Color BorderStrong = Color.FromArgb(201, 209, 217);
        public static readonly Color Ink = Color.FromArgb(25, 35, 45);
        public static readonly Color Muted = Color.FromArgb(96, 109, 122);
        public static readonly Color Faint = Color.FromArgb(139, 150, 161);
        public static readonly Color Accent = Color.FromArgb(13, 122, 100);
        public static readonly Color AccentHover = Color.FromArgb(9, 103, 84);
        public static readonly Color AccentSoft = Color.FromArgb(232, 246, 241);
        public static readonly Color AccentBorder = Color.FromArgb(137, 205, 188);
        public static readonly Color Warning = Color.FromArgb(174, 91, 20);
        public static readonly Color WarningSoft = Color.FromArgb(255, 247, 235);
        public static readonly Color WarningBorder = Color.FromArgb(239, 205, 157);
        public static readonly Color Danger = Color.FromArgb(179, 38, 30);
        public static readonly Color DangerSoft = Color.FromArgb(254, 242, 241);
        public static readonly Color DangerBorder = Color.FromArgb(235, 179, 174);
        public static readonly Color Disabled = Color.FromArgb(203, 210, 217);

        public static readonly Font Body = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        public static readonly Font BodySemibold = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        public static readonly Font Small = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular);
        public static readonly Font SmallSemibold = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Bold);
        public static readonly Font Heading = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
        public static readonly Font PageTitle = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold);
        public static readonly Font Display = new Font("Microsoft YaHei UI", 30F, FontStyle.Bold);
        public static readonly Font Metric = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);

        public static int Scale(Control control, int value)
        {
            return (int)System.Math.Round(value * control.DeviceDpi / 96.0);
        }

        public static float Scale(Control control, float value)
        {
            return (float)(value * control.DeviceDpi / 96.0);
        }

        public static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            if (diameter <= 0)
            {
                path.AddRectangle(rectangle);
                return path;
            }

            var arc = new RectangleF(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void ConfigureGraphics(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        }
    }
}
