using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal sealed class AppMarkControl : Control
    {
        public AppMarkControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            var bounds = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var background = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 8F)))
            using (var brush = new SolidBrush(AppTheme.Ink))
            {
                e.Graphics.FillPath(brush, background);
            }

            var inset = AppTheme.Scale(this, 8F);
            var ledger = new RectangleF(
                inset,
                AppTheme.Scale(this, 10F),
                Width - inset * 2,
                Height - AppTheme.Scale(this, 20F));
            using (var outline = AppTheme.RoundedRectangle(ledger, AppTheme.Scale(this, 4F)))
            using (var pen = new Pen(Color.FromArgb(242, 247, 248), AppTheme.Scale(this, 1.8F)))
            {
                e.Graphics.DrawPath(pen, outline);
            }

            var fillWidth = ledger.Width * 0.64F;
            using (var fill = AppTheme.RoundedRectangle(
                new RectangleF(ledger.X + 2, ledger.Y + 2, fillWidth, ledger.Height - 4),
                AppTheme.Scale(this, 2.6F)))
            using (var brush = new SolidBrush(AppTheme.Accent))
            {
                e.Graphics.FillPath(brush, fill);
            }

            using (var brush = new SolidBrush(Color.FromArgb(230, 236, 239)))
            {
                var dotSize = AppTheme.Scale(this, 2.2F);
                e.Graphics.FillEllipse(
                    brush,
                    ledger.Right - dotSize - AppTheme.Scale(this, 3F),
                    ledger.Bottom - dotSize - AppTheme.Scale(this, 3F),
                    dotSize,
                    dotSize);
            }
        }
    }
}
