using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;

namespace DiskFill.UI.Controls
{
    internal enum DriveRunState
    {
        Running = 0,
        Paused = 1,
        Completed = 2,
        Failed = 3
    }

    internal sealed class DriveProgressCard : Control
    {
        private readonly FillJob _job;
        private readonly string _physicalDiskDescription;
        private readonly int _index;
        private long _writtenBytes;
        private double _bytesPerSecond;
        private double? _remainingSeconds;
        private DriveRunState _state;

        public DriveProgressCard(
            int index,
            FillJob job,
            string physicalDiskDescription)
        {
            _index = index;
            _job = job ?? throw new ArgumentNullException(nameof(job));
            _physicalDiskDescription = physicalDiskDescription ?? "物理磁盘未知";
            _writtenBytes = job.WrittenBytes;
            Height = 112;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            AccessibleName = "磁盘 " + job.VolumeRoot + " 写入进度";
        }

        public FillJob Job
        {
            get { return _job; }
        }

        public void UpdateProgress(
            long writtenBytes,
            double bytesPerSecond,
            double? remainingSeconds,
            DriveRunState state)
        {
            _writtenBytes = Math.Max(0, writtenBytes);
            _bytesPerSecond = Math.Max(0, bytesPerSecond);
            _remainingSeconds = remainingSeconds;
            _state = state;
            AccessibleDescription = GetAccessibleDescription();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.ConfigureGraphics(e.Graphics);

            var colors = GetColors();
            var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
            using (var path = AppTheme.RoundedRectangle(bounds, AppTheme.Scale(this, 7F)))
            using (var background = new SolidBrush(colors.Background))
            using (var border = new Pen(colors.Border))
            {
                e.Graphics.FillPath(background, path);
                e.Graphics.DrawPath(border, path);
            }

            var badgeSize = AppTheme.Scale(this, 24);
            var badgeBounds = new Rectangle(
                AppTheme.Scale(this, 16),
                AppTheme.Scale(this, 15),
                badgeSize,
                badgeSize);
            using (var brush = new SolidBrush(colors.Accent))
            {
                e.Graphics.FillEllipse(brush, badgeBounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                _index.ToString(),
                AppTheme.SmallSemibold,
                badgeBounds,
                Color.White,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);

            var titleLeft = AppTheme.Scale(this, 50);
            var rightWidth = AppTheme.Scale(this, 126);
            var titleWidth = Math.Max(
                50,
                Width - titleLeft - rightWidth - AppTheme.Scale(this, 18));
            TextRenderer.DrawText(
                e.Graphics,
                _job.VolumeRoot.TrimEnd('\\') + "  " + _job.VolumeLabel,
                AppTheme.BodySemibold,
                new Rectangle(
                    titleLeft,
                    AppTheme.Scale(this, 12),
                    titleWidth,
                    AppTheme.Scale(this, 23)),
                AppTheme.Ink,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                _physicalDiskDescription + "  ·  " + _job.FileSystem + "  ·  "
                    + ByteFormatter.Format(_job.PlannedBytes),
                AppTheme.Small,
                new Rectangle(
                    titleLeft,
                    AppTheme.Scale(this, 35),
                    titleWidth,
                    AppTheme.Scale(this, 18)),
                AppTheme.Muted,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);

            var fraction = _job.PlannedBytes <= 0
                ? 0
                : Math.Max(0, Math.Min(1, (double)_writtenBytes / _job.PlannedBytes));
            var rightLeft = Width - rightWidth - AppTheme.Scale(this, 16);
            TextRenderer.DrawText(
                e.Graphics,
                ((int)Math.Floor(fraction * 100)).ToString() + "%",
                AppTheme.Metric,
                new Rectangle(
                    rightLeft,
                    AppTheme.Scale(this, 10),
                    rightWidth,
                    AppTheme.Scale(this, 27)),
                colors.Accent,
                TextFormatFlags.Right
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                GetStateText(),
                AppTheme.Small,
                new Rectangle(
                    rightLeft,
                    AppTheme.Scale(this, 36),
                    rightWidth,
                    AppTheme.Scale(this, 18)),
                colors.Muted,
                TextFormatFlags.Right
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding);

            var barBounds = new Rectangle(
                AppTheme.Scale(this, 16),
                AppTheme.Scale(this, 62),
                Width - AppTheme.Scale(this, 32),
                AppTheme.Scale(this, 8));
            using (var trackPath = AppTheme.RoundedRectangle(barBounds, barBounds.Height / 2F))
            using (var trackBrush = new SolidBrush(AppTheme.Border))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
            }

            if (fraction > 0)
            {
                var fillWidth = Math.Max(barBounds.Height, barBounds.Width * (float)fraction);
                var fillBounds = new Rectangle(
                    barBounds.X,
                    barBounds.Y,
                    (int)Math.Min(barBounds.Width, fillWidth),
                    barBounds.Height);
                using (var fillPath = AppTheme.RoundedRectangle(fillBounds, fillBounds.Height / 2F))
                using (var fillBrush = new SolidBrush(colors.Accent))
                {
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            }

            var bottomTop = AppTheme.Scale(this, 78);
            var bottomHeight = AppTheme.Scale(this, 20);
            var firstWidth = (int)(Width * 0.42F);
            var secondLeft = firstWidth;
            var secondWidth = (int)(Width * 0.27F);
            var thirdLeft = secondLeft + secondWidth;
            TextRenderer.DrawText(
                e.Graphics,
                "已写入 " + ByteFormatter.Format(_writtenBytes)
                    + " / " + ByteFormatter.Format(_job.PlannedBytes),
                AppTheme.Small,
                new Rectangle(AppTheme.Scale(this, 16), bottomTop, firstWidth, bottomHeight),
                AppTheme.Muted,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                "速度 " + ByteFormatter.FormatRate(_bytesPerSecond),
                AppTheme.Small,
                new Rectangle(secondLeft, bottomTop, secondWidth, bottomHeight),
                AppTheme.Muted,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                "剩余 " + GetRemainingText(),
                AppTheme.Small,
                new Rectangle(
                    thirdLeft,
                    bottomTop,
                    Width - thirdLeft - AppTheme.Scale(this, 16),
                    bottomHeight),
                AppTheme.Muted,
                TextFormatFlags.Right
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding);
        }

        private string GetStateText()
        {
            switch (_state)
            {
                case DriveRunState.Paused:
                    return "已暂停";
                case DriveRunState.Completed:
                    return "已完成";
                case DriveRunState.Failed:
                    return "已中断";
                default:
                    return _bytesPerSecond > 0 ? "顺序写入中" : "计算持续速度";
            }
        }

        private string GetRemainingText()
        {
            if (_state == DriveRunState.Completed)
            {
                return "0 分钟";
            }

            if (_state == DriveRunState.Failed)
            {
                return "--";
            }

            if (_remainingSeconds.HasValue)
            {
                return TimeFormatter.FormatEta(TimeSpan.FromSeconds(_remainingSeconds.Value));
            }

            return "--";
        }

        private string GetAccessibleDescription()
        {
            return _job.VolumeRoot + " " + GetStateText()
                + "，已写入 " + ByteFormatter.Format(_writtenBytes)
                + "，计划 " + ByteFormatter.Format(_job.PlannedBytes)
                + "，速度 " + ByteFormatter.FormatRate(_bytesPerSecond);
        }

        private CardColors GetColors()
        {
            switch (_state)
            {
                case DriveRunState.Paused:
                    return new CardColors(
                        AppTheme.WarningSoft,
                        AppTheme.WarningBorder,
                        AppTheme.Warning,
                        AppTheme.Warning);
                case DriveRunState.Completed:
                    return new CardColors(
                        Color.FromArgb(247, 252, 250),
                        AppTheme.AccentBorder,
                        AppTheme.Accent,
                        AppTheme.Muted);
                case DriveRunState.Failed:
                    return new CardColors(
                        AppTheme.DangerSoft,
                        AppTheme.DangerBorder,
                        AppTheme.Danger,
                        AppTheme.Danger);
                default:
                    return new CardColors(
                        AppTheme.Surface,
                        AppTheme.Border,
                        AppTheme.Accent,
                        AppTheme.Muted);
            }
        }

        private struct CardColors
        {
            public CardColors(Color background, Color border, Color accent, Color muted)
            {
                Background = background;
                Border = border;
                Accent = accent;
                Muted = muted;
            }

            public Color Background { get; private set; }

            public Color Border { get; private set; }

            public Color Accent { get; private set; }

            public Color Muted { get; private set; }
        }
    }
}
