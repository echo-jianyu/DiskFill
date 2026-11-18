using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DiskFill.Core.Models;

namespace DiskFill.UI.Controls
{
    internal sealed class ModeSelector : Control
    {
        private OverwriteMode _selectedMode;

        public ModeSelector()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable
                | ControlStyles.UserPaint,
                true);
            Font = AppTheme.SmallSemibold;
            Height = 38;
            TabStop = true;
            AccessibleRole = AccessibleRole.Grouping;
            Cursor = Cursors.Hand;
        }

        public event EventHandler SelectedModeChanged;

        public OverwriteMode SelectedMode
        {
            get { return _selectedMode; }
            set
            {
                if (_selectedMode == value)
                {
                    return;
                }

                _selectedMode = value;
                Invalidate();
                SelectedModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);
            DrawOption(
                e.Graphics,
                new Rectangle(0, 0, Width / 2 - 3, Height),
                "非零填充",
                SelectedMode == OverwriteMode.FastNonZero);
            DrawOption(
                e.Graphics,
                new Rectangle(Width / 2 + 3, 0, Width / 2 - 3, Height),
                "全零填充",
                SelectedMode == OverwriteMode.Zero);

            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(
                    e.Graphics,
                    Rectangle.Inflate(ClientRectangle, -AppTheme.Scale(this, 3), -AppTheme.Scale(this, 3)),
                    AppTheme.Ink,
                    AppTheme.Surface);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                SelectedMode = e.X < Width / 2 ? OverwriteMode.FastNonZero : OverwriteMode.Zero;
                Focus();
            }

            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                SelectedMode = OverwriteMode.FastNonZero;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                SelectedMode = OverwriteMode.Zero;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                SelectedMode = SelectedMode == OverwriteMode.Zero
                    ? OverwriteMode.FastNonZero
                    : OverwriteMode.Zero;
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

        private void DrawOption(Graphics graphics, Rectangle bounds, string text, bool selected)
        {
            var paletteBackground = selected && Enabled
                ? AppTheme.AccentSoft
                : AppTheme.Surface;
            var paletteBorder = selected && Enabled
                ? AppTheme.AccentBorder
                : AppTheme.Border;
            var paletteText = !Enabled
                ? AppTheme.Faint
                : (selected ? AppTheme.Accent : AppTheme.Muted);

            using (var path = AppTheme.RoundedRectangle(
                new RectangleF(bounds.X + 0.5F, bounds.Y + 0.5F, bounds.Width - 1F, bounds.Height - 1F),
                AppTheme.Scale(this, 5F)))
            using (var background = new SolidBrush(paletteBackground))
            using (var border = new Pen(paletteBorder))
            {
                graphics.FillPath(background, path);
                graphics.DrawPath(border, path);
            }

            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                bounds,
                paletteText,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);
        }
    }
}
