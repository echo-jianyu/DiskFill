using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal sealed class RingProgress : Control
    {
        private double _value;
        private Color _progressColor = AppTheme.Accent;

        public RingProgress()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            TabStop = false;
        }

        public double Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(1, value));
                Invalidate();
            }
        }

        public Color ProgressColor
        {
            get { return _progressColor; }
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            var thickness = AppTheme.Scale(this, 11F);
            var margin = thickness / 2F + AppTheme.Scale(this, 5F);
            var size = Math.Min(Width, Height) - margin * 2;
            var ring = new RectangleF(
                (Width - size) / 2F,
                (Height - size) / 2F,
                size,
                size);

            using (var track = new Pen(AppTheme.Border, thickness))
            {
                track.StartCap = LineCap.Round;
                track.EndCap = LineCap.Round;
                e.Graphics.DrawArc(track, ring, -90, 360);
            }

            if (_value > 0)
            {
                using (var progress = new Pen(_progressColor, thickness))
                {
                    progress.StartCap = LineCap.Round;
                    progress.EndCap = LineCap.Round;
                    e.Graphics.DrawArc(progress, ring, -90, (float)(360 * _value));
                }
            }

            var percent = ((int)Math.Floor(_value * 100)).ToString() + "%";
            var percentBounds = new Rectangle(
                0,
                Height / 2 - AppTheme.Scale(this, 31),
                Width,
                AppTheme.Scale(this, 50));
            TextRenderer.DrawText(
                e.Graphics,
                percent,
                AppTheme.Display,
                percentBounds,
                AppTheme.Ink,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);

            TextRenderer.DrawText(
                e.Graphics,
                "已写入",
                AppTheme.Small,
                new Rectangle(0, Height / 2 + AppTheme.Scale(this, 19), Width, AppTheme.Scale(this, 22)),
                AppTheme.Muted,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);
        }
    }
}
