using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal sealed class NoticeBar : Control
    {
        private PillTone _tone = PillTone.Warning;

        public NoticeBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            Font = AppTheme.Small;
            Height = 68;
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

        protected override void OnTextChanged(System.EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);
            var colors = GetColors();
            var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);

            using (var path = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 6F)))
            using (var background = new SolidBrush(colors.Background))
            using (var border = new Pen(colors.Border))
            {
                e.Graphics.FillPath(background, path);
                e.Graphics.DrawPath(border, path);
            }

            var iconSize = AppTheme.Scale(this, 18);
            var iconRect = new Rectangle(
                AppTheme.Scale(this, 16),
                (Height - iconSize) / 2,
                iconSize,
                iconSize);
            using (var pen = new Pen(colors.Foreground, AppTheme.Scale(this, 1.7F)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                e.Graphics.DrawEllipse(pen, iconRect);
                var midX = iconRect.Left + iconRect.Width / 2F;
                e.Graphics.DrawLine(
                    pen,
                    midX,
                    iconRect.Top + iconRect.Height * 0.26F,
                    midX,
                    iconRect.Top + iconRect.Height * 0.56F);
                using (var brush = new SolidBrush(colors.Foreground))
                {
                    var dot = AppTheme.Scale(this, 2F);
                    e.Graphics.FillEllipse(
                        brush,
                        midX - dot / 2F,
                        iconRect.Bottom - iconRect.Height * 0.25F,
                        dot,
                        dot);
                }
            }

            var textBounds = new Rectangle(
                AppTheme.Scale(this, 46),
                AppTheme.Scale(this, 10),
                Width - AppTheme.Scale(this, 62),
                Height - AppTheme.Scale(this, 20));
            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                textBounds,
                colors.Foreground,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.WordBreak
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
        }

        private NoticeColors GetColors()
        {
            switch (_tone)
            {
                case PillTone.Accent:
                    return new NoticeColors(AppTheme.AccentSoft, AppTheme.AccentBorder, AppTheme.Accent);
                case PillTone.Danger:
                    return new NoticeColors(AppTheme.DangerSoft, AppTheme.DangerBorder, AppTheme.Danger);
                default:
                    return new NoticeColors(AppTheme.WarningSoft, AppTheme.WarningBorder, AppTheme.Warning);
            }
        }

        private struct NoticeColors
        {
            public NoticeColors(Color background, Color border, Color foreground)
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
