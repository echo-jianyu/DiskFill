using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;

namespace DiskFill.UI.Controls
{
    internal sealed class DriveCard : Control
    {
        private bool _hovered;
        private bool _selected;
        private JobInspection _inspection;

        public DriveCard(DriveTarget drive)
        {
            Drive = drive ?? throw new ArgumentNullException(nameof(drive));
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable
                | ControlStyles.UserPaint,
                true);
            Height = 108;
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.ListItem;
            AccessibleName = "磁盘 " + drive.DisplayName + " " + drive.Label;
            AccessibleDescription = GetAccessibleDescription();
        }

        public DriveTarget Drive { get; private set; }

        public JobInspection Inspection
        {
            get { return _inspection; }
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value)
                {
                    return;
                }

                _selected = value;
                Invalidate();
            }
        }

        public void SetInspection(JobInspection inspection)
        {
            _inspection = inspection;
            AccessibleDescription = GetAccessibleDescription();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            var background = _selected
                ? Color.FromArgb(247, 252, 250)
                : (_hovered ? AppTheme.SurfaceMuted : AppTheme.Surface);
            var border = _selected ? AppTheme.Accent : AppTheme.Border;
            var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
            using (var path = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 7F)))
            using (var brush = new SolidBrush(background))
            using (var pen = new Pen(border, _selected ? AppTheme.Scale(this, 1.5F) : 1F))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            if (_selected)
            {
                using (var brush = new SolidBrush(AppTheme.Accent))
                {
                    e.Graphics.FillRectangle(
                        brush,
                        1,
                        AppTheme.Scale(this, 18),
                        AppTheme.Scale(this, 3),
                        Height - AppTheme.Scale(this, 36));
                }
            }

            var iconRect = new Rectangle(
                AppTheme.Scale(this, 18),
                (Height - AppTheme.Scale(this, 42)) / 2,
                AppTheme.Scale(this, 42),
                AppTheme.Scale(this, 42));
            DrawDriveGlyph(e.Graphics, iconRect);

            var rightWidth = AppTheme.Scale(this, 132);
            var textLeft = AppTheme.Scale(this, 74);
            var textRight = Width - rightWidth - AppTheme.Scale(this, 16);
            var textWidth = Math.Max(0, textRight - textLeft);
            var titleBounds = new Rectangle(textLeft, AppTheme.Scale(this, 17), textWidth, AppTheme.Scale(this, 25));
            TextRenderer.DrawText(
                e.Graphics,
                Drive.DisplayName + "   " + Drive.Label,
                AppTheme.BodySemibold,
                titleBounds,
                Drive.IsEligible ? AppTheme.Ink : AppTheme.Muted,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);

            var details = Drive.MediaDescription + "  ·  " + Drive.FileSystem + "  ·  "
                + ByteFormatter.Format(Drive.TotalBytes);
            TextRenderer.DrawText(
                e.Graphics,
                details,
                AppTheme.Small,
                new Rectangle(textLeft, AppTheme.Scale(this, 44), textWidth, AppTheme.Scale(this, 19)),
                AppTheme.Muted,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);

            var thirdLine = GetThirdLine();
            if (!string.IsNullOrEmpty(thirdLine))
            {
                var thirdColor = !Drive.IsEligible || (_inspection != null && _inspection.HasIssue)
                    ? AppTheme.Warning
                    : AppTheme.Faint;
                TextRenderer.DrawText(
                    e.Graphics,
                    thirdLine,
                    AppTheme.Small,
                    new Rectangle(textLeft, AppTheme.Scale(this, 67), textWidth, AppTheme.Scale(this, 20)),
                    thirdColor,
                    TextFormatFlags.Left
                    | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.SingleLine
                    | TextFormatFlags.EndEllipsis
                    | TextFormatFlags.NoPadding);
            }

            DrawRightDetails(e.Graphics, rightWidth);

            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(
                    e.Graphics,
                    Rectangle.Inflate(ClientRectangle, -AppTheme.Scale(this, 5), -AppTheme.Scale(this, 5)),
                    AppTheme.Ink,
                    background);
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
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        private void DrawRightDetails(Graphics graphics, int rightWidth)
        {
            var right = Width - rightWidth - AppTheme.Scale(this, 20);
            TextRenderer.DrawText(
                graphics,
                ByteFormatter.Format(Drive.AvailableBytes),
                AppTheme.BodySemibold,
                new Rectangle(right, AppTheme.Scale(this, 22), rightWidth, AppTheme.Scale(this, 25)),
                Drive.IsEligible ? AppTheme.Ink : AppTheme.Muted,
                TextFormatFlags.Right
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                graphics,
                "可用空间",
                AppTheme.Small,
                new Rectangle(right, AppTheme.Scale(this, 46), rightWidth, AppTheme.Scale(this, 18)),
                AppTheme.Faint,
                TextFormatFlags.Right
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);

            var pillText = GetPillText();
            if (string.IsNullOrEmpty(pillText))
            {
                return;
            }

            var pillSize = TextRenderer.MeasureText(pillText, AppTheme.SmallSemibold);
            var pillWidth = pillSize.Width + AppTheme.Scale(this, 14);
            var pillRect = new Rectangle(
                right + rightWidth - pillWidth,
                AppTheme.Scale(this, 70),
                pillWidth,
                AppTheme.Scale(this, 22));
            var tone = GetPillColors(
                _inspection != null && _inspection.HasIssue
                    ? PillTone.Warning
                    : (Drive.IsEligible || (_inspection != null && _inspection.HasJob)
                        ? (Drive.MediaKind == MediaKind.Unknown ? PillTone.Warning : PillTone.Accent)
                        : PillTone.Warning));
            using (var path = AppTheme.RoundedRectangle(pillRect, AppTheme.Scale(this, 10F)))
            using (var brush = new SolidBrush(tone.Background))
            using (var pen = new Pen(tone.Border))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                graphics,
                pillText,
                AppTheme.SmallSemibold,
                pillRect,
                tone.Foreground,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);
        }

        private void DrawDriveGlyph(Graphics graphics, Rectangle bounds)
        {
            var color = Drive.IsEligible ? AppTheme.Accent : AppTheme.Faint;
            using (var background = new SolidBrush(Drive.IsEligible ? AppTheme.AccentSoft : AppTheme.SurfaceMuted))
            using (var path = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 6F)))
            {
                graphics.FillPath(background, path);
            }

            var drive = new RectangleF(
                bounds.Left + bounds.Width * 0.16F,
                bounds.Top + bounds.Height * 0.26F,
                bounds.Width * 0.68F,
                bounds.Height * 0.48F);
            using (var path = AppTheme.RoundedRectangle(drive, AppTheme.Scale(this, 3F)))
            using (var pen = new Pen(color, AppTheme.Scale(this, 1.7F)))
            {
                graphics.DrawPath(pen, path);
            }

            using (var pen = new Pen(color, AppTheme.Scale(this, 1.4F)))
            {
                graphics.DrawLine(
                    pen,
                    drive.Left + drive.Width * 0.12F,
                    drive.Bottom - drive.Height * 0.24F,
                    drive.Right - drive.Width * 0.12F,
                    drive.Bottom - drive.Height * 0.24F);
            }

            using (var brush = new SolidBrush(color))
            {
                var dot = AppTheme.Scale(this, 2.6F);
                graphics.FillEllipse(
                    brush,
                    drive.Right - dot - AppTheme.Scale(this, 3F),
                    drive.Bottom - dot - AppTheme.Scale(this, 3F),
                    dot,
                    dot);
            }
        }

        private string GetThirdLine()
        {
            if (_inspection != null && _inspection.HasIssue)
            {
                return _inspection.Message;
            }

            if (!Drive.IsEligible)
            {
                return Drive.IneligibleReason;
            }

            if (_inspection != null && _inspection.HasJob)
            {
                if (_inspection.Job.State == FillJobState.Completed)
                {
                    return "覆写已完成 · 填充文件仍保留";
                }

                return "可继续上次任务 · 已写入 " + ByteFormatter.Format(_inspection.Job.WrittenBytes);
            }

            if (Drive.MediaKind == MediaKind.Unknown)
            {
                return "无法确认介质类型，开始前请确认这是机械硬盘";
            }

            return "顺序覆写可用簇 · 预计 " + ByteFormatter.Format(Drive.AvailableBytes) + " 写入量";
        }

        private string GetPillText()
        {
            if (_inspection != null && _inspection.HasIssue)
            {
                return "需检查";
            }

            if (_inspection != null && _inspection.HasJob)
            {
                if (_inspection.Job.State == FillJobState.Completed)
                {
                    return "待清理";
                }

                return "可续写";
            }

            if (!Drive.IsEligible)
            {
                if (Drive.MediaKind == MediaKind.SolidState)
                {
                    return "不适用 SSD";
                }

                return "受保护";
            }

            if (Drive.MediaKind == MediaKind.Unknown)
            {
                return "需确认";
            }

            return "可写入";
        }

        private string GetAccessibleDescription()
        {
            return Drive.MediaDescription
                + "，文件系统 " + Drive.FileSystem
                + "，可用 " + ByteFormatter.Format(Drive.AvailableBytes)
                + (Drive.IsEligible ? "。" : "。" + Drive.IneligibleReason + "。");
        }

        private static PillColors GetPillColors(PillTone tone)
        {
            switch (tone)
            {
                case PillTone.Accent:
                    return new PillColors(AppTheme.AccentSoft, AppTheme.AccentBorder, AppTheme.Accent);
                case PillTone.Warning:
                    return new PillColors(AppTheme.WarningSoft, AppTheme.WarningBorder, AppTheme.Warning);
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
