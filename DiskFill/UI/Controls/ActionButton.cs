using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal enum ButtonStyle
    {
        Primary,
        Secondary,
        Danger,
        Ghost
    }

    internal enum ButtonGlyph
    {
        None,
        Play,
        Pause,
        Stop,
        Refresh,
        Delete
    }

    internal sealed class ActionButton : Control, IButtonControl
    {
        private bool _hovered;
        private bool _pressed;
        private ButtonStyle _style;
        private ButtonGlyph _glyph;

        public ActionButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable
                | ControlStyles.UserPaint,
                true);
            Font = AppTheme.BodySemibold;
            Height = 44;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
        }

        public ButtonStyle Style
        {
            get { return _style; }
            set
            {
                _style = value;
                Invalidate();
            }
        }

        public ButtonGlyph Glyph
        {
            get { return _glyph; }
            set
            {
                _glyph = value;
                Invalidate();
            }
        }

        public DialogResult DialogResult { get; set; }

        public void NotifyDefault(bool value)
        {
            Invalidate();
        }

        public void PerformClick()
        {
            if (Enabled)
            {
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            var palette = GetPalette();
            var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
            using (var path = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 6F)))
            {
                using (var brush = new SolidBrush(palette.Background))
                {
                    e.Graphics.FillPath(brush, path);
                }

                if (palette.Border.A > 0)
                {
                    using (var pen = new Pen(palette.Border, AppTheme.Scale(this, 1F)))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            var textSize = TextRenderer.MeasureText(Text ?? string.Empty, Font);
            var iconSize = _glyph == ButtonGlyph.None ? 0 : AppTheme.Scale(this, 17);
            var gap = _glyph == ButtonGlyph.None ? 0 : AppTheme.Scale(this, 8);
            var contentWidth = textSize.Width + iconSize + gap;
            var contentX = (Width - contentWidth) / 2;
            var textBounds = new Rectangle(
                contentX + iconSize + gap,
                0,
                Width - contentX - iconSize - gap,
                Height);

            if (iconSize > 0)
            {
                DrawGlyph(
                    e.Graphics,
                    new Rectangle(
                        contentX,
                        (Height - iconSize) / 2,
                        iconSize,
                        iconSize),
                    palette.Foreground);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                textBounds,
                palette.Foreground,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);

            if (Focused && ShowFocusCues)
            {
                var focus = Rectangle.Inflate(ClientRectangle, -AppTheme.Scale(this, 4), -AppTheme.Scale(this, 4));
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, palette.Foreground, palette.Background);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _pressed = true;
                Focus();
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                PerformClick();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        private ButtonPalette GetPalette()
        {
            if (!Enabled)
            {
                return new ButtonPalette(
                    AppTheme.SurfaceMuted,
                    AppTheme.Border,
                    AppTheme.Faint);
            }

            switch (_style)
            {
                case ButtonStyle.Secondary:
                    return new ButtonPalette(
                        _pressed ? AppTheme.SurfaceMuted : (_hovered ? AppTheme.SurfaceMuted : AppTheme.Surface),
                        AppTheme.BorderStrong,
                        AppTheme.Ink);
                case ButtonStyle.Danger:
                    return new ButtonPalette(
                        _pressed ? Color.FromArgb(154, 29, 23) : (_hovered ? Color.FromArgb(160, 32, 26) : AppTheme.Danger),
                        Color.Empty,
                        Color.White);
                case ButtonStyle.Ghost:
                    return new ButtonPalette(
                        _pressed ? AppTheme.SurfaceMuted : (_hovered ? AppTheme.SurfaceMuted : Color.Transparent),
                        Color.Empty,
                        AppTheme.Muted);
                default:
                    return new ButtonPalette(
                        _pressed ? AppTheme.AccentHover : (_hovered ? AppTheme.AccentHover : AppTheme.Accent),
                        Color.Empty,
                        Color.White);
            }
        }

        private void DrawGlyph(Graphics graphics, Rectangle bounds, Color color)
        {
            using (var pen = new Pen(color, AppTheme.Scale(this, 1.8F)))
            using (var brush = new SolidBrush(color))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                var midX = bounds.Left + bounds.Width / 2F;
                var midY = bounds.Top + bounds.Height / 2F;
                switch (_glyph)
                {
                    case ButtonGlyph.Play:
                        graphics.FillPolygon(
                            brush,
                            new[]
                            {
                                new PointF(bounds.Left + bounds.Width * 0.31F, bounds.Top + bounds.Height * 0.2F),
                                new PointF(bounds.Left + bounds.Width * 0.31F, bounds.Top + bounds.Height * 0.8F),
                                new PointF(bounds.Left + bounds.Width * 0.8F, midY)
                            });
                        break;
                    case ButtonGlyph.Pause:
                        var barWidth = AppTheme.Scale(this, 3F);
                        graphics.FillRectangle(
                            brush,
                            bounds.Left + bounds.Width * 0.28F,
                            bounds.Top + bounds.Height * 0.2F,
                            barWidth,
                            bounds.Height * 0.6F);
                        graphics.FillRectangle(
                            brush,
                            bounds.Right - bounds.Width * 0.28F - barWidth,
                            bounds.Top + bounds.Height * 0.2F,
                            barWidth,
                            bounds.Height * 0.6F);
                        break;
                    case ButtonGlyph.Stop:
                        var side = bounds.Width * 0.48F;
                        graphics.FillRectangle(
                            brush,
                            midX - side / 2F,
                            midY - side / 2F,
                            side,
                            side);
                        break;
                    case ButtonGlyph.Refresh:
                        var refreshBounds = RectangleF.Inflate(bounds, -bounds.Width * 0.18F, -bounds.Height * 0.18F);
                        graphics.DrawArc(pen, refreshBounds, 35, 285);
                        graphics.FillPolygon(
                            brush,
                            new[]
                            {
                                new PointF(refreshBounds.Right - 1, refreshBounds.Top + 1),
                                new PointF(refreshBounds.Right - 5, refreshBounds.Top - 3),
                                new PointF(refreshBounds.Right + 2, refreshBounds.Top - 3)
                            });
                        break;
                    case ButtonGlyph.Delete:
                        var lidY = bounds.Top + bounds.Height * 0.28F;
                        graphics.DrawLine(
                            pen,
                            bounds.Left + bounds.Width * 0.22F,
                            lidY,
                            bounds.Right - bounds.Width * 0.22F,
                            lidY);
                        graphics.DrawLine(
                            pen,
                            bounds.Left + bounds.Width * 0.38F,
                            bounds.Top + bounds.Height * 0.16F,
                            bounds.Right - bounds.Width * 0.38F,
                            bounds.Top + bounds.Height * 0.16F);
                        graphics.DrawRectangle(
                            pen,
                            bounds.Left + bounds.Width * 0.3F,
                            bounds.Top + bounds.Height * 0.36F,
                            bounds.Width * 0.4F,
                            bounds.Height * 0.46F);
                        break;
                }
            }
        }

        private struct ButtonPalette
        {
            public ButtonPalette(Color background, Color border, Color foreground)
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
