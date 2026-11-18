using System.Drawing;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal sealed class MetricView : Control
    {
        private string _caption = string.Empty;
        private string _value = "--";
        private string _note = string.Empty;

        public MetricView()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            Height = 76;
            TabStop = false;
        }

        public string Caption
        {
            get { return _caption; }
            set
            {
                _caption = value ?? string.Empty;
                Invalidate();
            }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                _value = value ?? "--";
                Invalidate();
            }
        }

        public string Note
        {
            get { return _note; }
            set
            {
                _note = value ?? string.Empty;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            TextRenderer.DrawText(
                e.Graphics,
                Caption,
                AppTheme.Small,
                new Rectangle(0, AppTheme.Scale(this, 2), Width, AppTheme.Scale(this, 18)),
                AppTheme.Faint,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                Value,
                AppTheme.Metric,
                new Rectangle(0, AppTheme.Scale(this, 21), Width, AppTheme.Scale(this, 31)),
                AppTheme.Ink,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                Note,
                AppTheme.Small,
                new Rectangle(0, AppTheme.Scale(this, 53), Width, AppTheme.Scale(this, 18)),
                AppTheme.Muted,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
        }
    }
}
