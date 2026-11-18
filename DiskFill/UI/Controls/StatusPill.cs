using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal enum PillTone
    {
        Neutral,
        Accent,
        Warning,
        Danger
    }

    internal sealed class StatusPill : Control
    {
        private PillTone _tone;

        public StatusPill()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            Font = AppTheme.SmallSemibold;
            Height = 26;
            Width = 76;
            TabStop = false;
        }

        public PillTone Tone
        {
            get { return _tone; }
            set
            {
                _tone = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            var colors = GetColors();
            var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
            using (var path = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 12F)))
            using (var background = new SolidBrush(colors.Background))
            using (var border = new Pen(colors.Border))
            {
                e.Graphics.FillPath(background, path);
                e.Graphics.DrawPath(border, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                ClientRectangle,
                colors.Foreground,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);
        }

        private PillColors GetColors()
        {
            switch (_tone)
            {
                case PillTone.Accent:
                    return new PillColors(AppTheme.AccentSoft, AppTheme.AccentBorder, AppTheme.Accent);
                case PillTone.Warning:
                    return new PillColors(AppTheme.WarningSoft, AppTheme.WarningBorder, AppTheme.Warning);
                case PillTone.Danger:
                    return new PillColors(AppTheme.DangerSoft, AppTheme.DangerBorder, AppTheme.Danger);
                default:
                    return new PillColors(AppTheme.SurfaceMuted, AppTheme.Border, AppTheme.Muted);
            }
        }

        private struct PillColors
        {
            public PillColors(Color background, Color border, Color foreground)
            {
                Background = background;
                Border = border;
                Foreground = foreground;
            }

            public Color Background { get; private set; }

            public Color Border { get; private set; }

            public Color Foreground { get; private set; }
        }
    }
}
