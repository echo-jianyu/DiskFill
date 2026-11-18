using System;
using System.Drawing;
using System.Windows.Forms;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;
using DiskFill.UI.Controls;

namespace DiskFill.UI.Dialogs
{
    internal sealed class ConfirmFillDialog : Form
    {
        private readonly DriveTarget _drive;
        private readonly TextBox _confirmationTextBox;
        private readonly ActionButton _confirmButton;

        public ConfirmFillDialog(DriveTarget drive)
        {
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
            if (string.IsNullOrEmpty(drive.DriveLetter))
            {
                throw new ArgumentException("The target drive has no drive letter.", nameof(drive));
            }

            InitializeWindow();
            _confirmationTextBox = CreateConfirmationTextBox();
            _confirmButton = CreateConfirmButton();
            BuildLayout(CreateCancelButton());

            _confirmationTextBox.TextChanged += OnConfirmationTextChanged;
            AcceptButton = _confirmButton;
            Shown += delegate { _confirmationTextBox.Focus(); };
        }

        private void InitializeWindow()
        {
            Text = "确认开始覆写";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = AppTheme.Surface;
            ClientSize = new Size(520, 456);
            Font = AppTheme.Body;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
        }

        private TextBox CreateConfirmationTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 2,
                CharacterCasing = CharacterCasing.Upper,
                AccessibleName = "确认盘符",
                Margin = new Padding(0)
            };
        }

        private ActionButton CreateConfirmButton()
        {
            var button = new ActionButton
            {
                Style = ButtonStyle.Danger,
                Glyph = ButtonGlyph.Play,
                Text = "开始覆写",
                Size = new Size(152, 42),
                Enabled = false,
                Margin = Padding.Empty
            };
            button.Click += OnConfirmClicked;
            return button;
        }

        private ActionButton CreateCancelButton()
        {
            var button = new ActionButton
            {
                Style = ButtonStyle.Secondary,
                Text = "取消",
                Size = new Size(104, 42),
                DialogResult = DialogResult.Cancel,
                Margin = Padding.Empty
            };
            button.Click += delegate { Close(); };
            CancelButton = button;
            return button;
        }

        private void BuildLayout(ActionButton cancelButton)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                ColumnCount = 1,
                RowCount = 10,
                Padding = new Padding(28, 22, 28, 20),
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            layout.Controls.Add(CreateHeader(), 0, 0);
            layout.Controls.Add(CreateSummary(), 0, 2);

            var instruction = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.BodySemibold,
                ForeColor = AppTheme.Ink,
                Text = "请输入盘符 " + _drive.DriveLetter.Substring(0, 1) + " 以确认",
                TextAlign = ContentAlignment.BottomLeft,
                Margin = Padding.Empty
            };
            layout.Controls.Add(instruction, 0, 4);
            layout.Controls.Add(_confirmationTextBox, 0, 5);

            var notice = new NoticeBar
            {
                Dock = DockStyle.Fill,
                Height = 62,
                Text = "请确认该盘已经格式化并完成备份。开始后请勿强制断电；中断后可继续写入。",
                Margin = Padding.Empty
            };
            layout.Controls.Add(notice, 0, 7);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty,
                Padding = new Padding(0, 4, 0, 0)
            };
            buttonPanel.Controls.Add(_confirmButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 9);

            Controls.Add(layout);
        }

        private Panel CreateHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty
            };
            var warningIcon = new WarningIconControl
            {
                Location = new Point(0, 2),
                Size = new Size(48, 48)
            };
            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
                ForeColor = AppTheme.Ink,
                Location = new Point(64, 1),
                Text = "确认覆写 " + _drive.DisplayName
            };
            var subtitle = new Label
            {
                AutoSize = false,
                Font = AppTheme.Body,
                ForeColor = AppTheme.Muted,
                Location = new Point(64, 33),
                Size = new Size(396, 28),
                Text = "已格式化的机械硬盘将按顺序写入，直到可用空间耗尽。"
            };
            header.Controls.Add(warningIcon);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            return header;
        }

        private SummaryPanel CreateSummary()
        {
            var summary = new SummaryPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            AddSummaryRow(summary, "目标磁盘", _drive.DisplayName + "  " + _drive.Label, 15);
            AddSummaryRow(summary, "可写入空间", ByteFormatter.Format(_drive.AvailableBytes), 45);
            AddSummaryRow(
                summary,
                "介质与文件系统",
                _drive.MediaDescription + " · " + _drive.FileSystem,
                75);
            return summary;
        }

        private void OnConfirmationTextChanged(object sender, EventArgs e)
        {
            var expected = _drive.DriveLetter.Substring(0, 1);
            _confirmButton.Enabled = string.Equals(
                _confirmationTextBox.Text.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase);
        }

        private void OnConfirmClicked(object sender, EventArgs e)
        {
            if (!_confirmButton.Enabled)
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static void AddSummaryRow(SummaryPanel panel, string caption, string value, int top)
        {
            var captionLabel = new Label
            {
                AutoSize = false,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Faint,
                Location = new Point(18, top + 2),
                Size = new Size(136, 22),
                Text = caption
            };
            var valueLabel = new Label
            {
                AutoSize = false,
                Font = AppTheme.BodySemibold,
                ForeColor = AppTheme.Ink,
                Location = new Point(160, top),
                Size = new Size(286, 24),
                TextAlign = ContentAlignment.MiddleRight,
                Text = value
            };
            panel.Controls.Add(captionLabel);
            panel.Controls.Add(valueLabel);
        }

        private sealed class SummaryPanel : Panel
        {
            public SummaryPanel()
            {
                BackColor = AppTheme.SurfaceMuted;
                SetStyle(ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (var pen = new Pen(AppTheme.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }

        private sealed class WarningIconControl : Control
        {
            public WarningIconControl()
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
                using (var background = new SolidBrush(AppTheme.WarningSoft))
                using (var pen = new Pen(AppTheme.WarningBorder, AppTheme.Scale(this, 1.4F)))
                {
                    e.Graphics.FillEllipse(background, 1, 1, Width - 3, Height - 3);
                    e.Graphics.DrawEllipse(pen, 1, 1, Width - 3, Height - 3);
                }

                using (var font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        "!",
                        font,
                        ClientRectangle,
                        AppTheme.Warning,
                        TextFormatFlags.HorizontalCenter
                        | TextFormatFlags.VerticalCenter
                        | TextFormatFlags.SingleLine
                        | TextFormatFlags.NoPadding);
                }
            }
        }
    }
}
