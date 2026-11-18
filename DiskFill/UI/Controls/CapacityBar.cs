using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal sealed class CapacityBar : Control
    {
        private double _value;
        private Color _progressColor = AppTheme.Accent;

        public CapacityBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            Height = 10;
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
            var radius = Height / 2F;

            using (var path = AppTheme.RoundedRectangle(
                new RectangleF(0, 0, Width, Height),
                radius))
            using (var brush = new SolidBrush(AppTheme.Border))
            {
                e.Graphics.FillPath(brush, path);
            }

            var fillWidth = (float)(Width * _value);
            if (fillWidth <= 0)
            {
                return;
            }

            using (var path = AppTheme.RoundedRectangle(
                new RectangleF(0, 0, Math.Max(Height, fillWidth), Height),
                radius))
            using (var brush = new SolidBrush(_progressColor))
            {
                e.Graphics.FillPath(brush, path);
            }
        }
    }
}
