using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;
using DiskFill.Core.Services;
using DiskFill.UI.Controls;
using DiskFill.UI.Dialogs;

namespace DiskFill.UI
{
    internal sealed class MainForm : Form
    {
        private readonly DriveService _driveService = new DriveService();
        private readonly JobStore _jobStore = new JobStore();
        private readonly FillEngine _fillEngine;
        private readonly List<DriveCard> _driveCards = new List<DriveCard>();

        private readonly ToolTip _toolTip = new ToolTip();
        private TableLayoutPanel _rootLayout;
        private Panel _topBar;
        private Panel _idleView;
        private Panel _runView;
        private DriveListPanel _driveFlow;
        private Label _driveCountLabel;
        private Label _footerStatusLabel;
        private ActionButton _refreshButton;
        private ActionButton _primaryButton;
        private ActionButton _cleanupButton;
        private ActionButton _pauseButton;
        private ActionButton _stopButton;
        private ModeSelector _modeSelector;
        private NoticeBar _sideNotice;
        private StatusPill _sidePill;
        private StatusPill _runPill;
        private Label _selectedDriveLabel;
        private Label _selectedMetaLabel;
        private Label _targetDetailValue;
        private Label _formatDetailValue;
        private Label _freeDetailValue;
        private Label _mediaDetailValue;
        private Label _modeDescriptionLabel;
        private CheckBox _deleteAfterCompletionCheckBox;
        private RingProgress _ringProgress;
        private CapacityBar _capacityBar;
        private Label _runTitleLabel;
        private Label _runSubtitleLabel;
        private Label _capacityStartLabel;
        private Label _capacityEndLabel;
        private MetricView _writtenMetric;
        private MetricView _speedMetric;
        private MetricView _elapsedMetric;
        private MetricView _etaMetric;

        private DriveTarget _selectedDrive;
        private JobInspection _selectedInspection;
        private FillJob _activeJob;
        private CancellationTokenSource _runCancellation;
        private PauseController _pauseController;
        private ExecutionStateScope _executionStateScope;
        private OverwriteMode _selectedMode = OverwriteMode.FastNonZero;
        private ScreenState _screenState = ScreenState.Idle;
        private bool _deleteAfterCompletion;
        private bool _isBusy;
        private bool _isEngineActive;
        private bool _closeAfterStop;
        private bool _suppressModeChange;

        public MainForm()
        {
            _fillEngine = new FillEngine(_jobStore, new DataPatternGenerator());
            InitializeWindow();
            BuildLayout();
            Shown += async delegate { await RefreshDrivesAsync(null); };
            FormClosing += OnFormClosing;
        }

        private enum ScreenState
        {
            Idle = 0,
            Running = 1,
            Paused = 2,
            Completed = 3,
            Failed = 4
        }

        private void InitializeWindow()
        {
            Text = "DiskFill - 机械硬盘覆写";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1180, 780);
            MinimumSize = new Size(1040, 720);
            BackColor = AppTheme.Canvas;
            ForeColor = AppTheme.Ink;
            Font = AppTheme.Body;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            KeyPreview = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not load the application icon.", exception);
            }
        }

        private void BuildLayout()
        {
            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            _topBar = CreateTopBar();
            var content = CreateContent();
            var footer = CreateFooter();

            _rootLayout.Controls.Add(_topBar, 0, 0);
            _rootLayout.Controls.Add(content, 0, 1);
            _rootLayout.Controls.Add(footer, 0, 2);
            Controls.Add(_rootLayout);
        }

        private Panel CreateTopBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty
            };
            bar.Paint += delegate (object sender, PaintEventArgs e)
            {
                using (var pen = new Pen(AppTheme.Border))
                {
                    e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
                }
            };

            var mark = new AppMarkControl
            {
                Location = new Point(28, 17),
                Size = new Size(38, 38)
            };
            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                ForeColor = AppTheme.Ink,
                Location = new Point(79, 18),
                Text = "DiskFill"
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Muted,
                Location = new Point(81, 43),
                Text = "机械硬盘顺序覆写"
            };

            bar.Controls.Add(mark);
            bar.Controls.Add(title);
            bar.Controls.Add(subtitle);
            return bar;
        }

        private Panel CreateContent()
        {
            var workspace = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Padding = new Padding(28, 20, 28, 22),
                Margin = Padding.Empty
            };

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            contentLayout.Controls.Add(CreatePageHeader(), 0, 0);
            contentLayout.Controls.Add(CreateWorkspaceBody(), 0, 1);
            workspace.Controls.Add(contentLayout);
            return workspace;
        }

        private Panel CreatePageHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            var title = new Label
            {
                AutoSize = true,
                Font = AppTheme.PageTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(0, 0),
                Text = "磁盘覆写"
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Font = AppTheme.Body,
                ForeColor = AppTheme.Muted,
                Location = new Point(2, 41),
                Text = "在快速格式化后的机械硬盘上顺序写入非零数据，覆盖仍可分配的数据簇。"
            };
            _refreshButton = new ActionButton
            {
                Style = ButtonStyle.Secondary,
                Glyph = ButtonGlyph.Refresh,
                Text = "刷新磁盘",
                Size = new Size(116, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(header.Width - 116, 5)
            };
            _refreshButton.Click += async delegate { await RefreshDrivesAsync(null); };
            header.Resize += delegate
            {
                _refreshButton.Location = new Point(header.Width - _refreshButton.Width, 5);
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(_refreshButton);
            return header;
        }

        private TableLayoutPanel CreateWorkspaceBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 364));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var leftHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            _idleView = CreateIdleView();
            _runView = CreateRunView();
            _runView.Visible = false;
            leftHost.Controls.Add(_runView);
            leftHost.Controls.Add(_idleView);

            body.Controls.Add(leftHost, 0, 0);
            body.Controls.Add(CreateSidePanel(), 2, 0);
            return body;
        }

        private Panel CreateIdleView()
        {
            var view = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var listHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            var listTitle = new Label
            {
                AutoSize = true,
                Font = AppTheme.Heading,
                ForeColor = AppTheme.Ink,
                Location = new Point(0, 2),
                Text = "可用磁盘"
            };
            _driveCountLabel = new Label
            {
                AutoSize = true,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Faint,
                Location = new Point(108, 9),
                Text = "正在扫描"
            };
            listHeader.Controls.Add(listTitle);
            listHeader.Controls.Add(_driveCountLabel);

            _driveFlow = new DriveListPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };

            layout.Controls.Add(listHeader, 0, 0);
            layout.Controls.Add(_driveFlow, 0, 1);
            view.Controls.Add(layout);
            return view;
        }

        private Panel CreateRunView()
        {
            var view = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

            var heading = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            _runTitleLabel = new Label
            {
                AutoSize = true,
                Font = AppTheme.PageTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(0, 0),
                Text = "正在覆写"
            };
            _runSubtitleLabel = new Label
            {
                AutoSize = true,
                Font = AppTheme.Body,
                ForeColor = AppTheme.Muted,
                Location = new Point(2, 43),
                Text = "顺序写入已开始"
            };
            _runPill = new StatusPill
            {
                Text = "运行中",
                Tone = PillTone.Accent,
                Size = new Size(76, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(heading.Width - 76, 9)
            };
            heading.Resize += delegate
            {
                _runPill.Location = new Point(heading.Width - _runPill.Width, 9);
            };
            heading.Controls.Add(_runTitleLabel);
            heading.Controls.Add(_runSubtitleLabel);
            heading.Controls.Add(_runPill);

            var progressTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            progressTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 246));
            progressTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            progressTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var ringHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            _ringProgress = new RingProgress
            {
                Size = new Size(214, 214),
                Location = new Point(4, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            ringHost.Controls.Add(_ringProgress);

            var metrics = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(10, 12, 0, 12),
                Padding = Padding.Empty
            };
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            _writtenMetric = new MetricView { Dock = DockStyle.Fill, Caption = "已写入", Margin = new Padding(0) };
            _speedMetric = new MetricView { Dock = DockStyle.Fill, Caption = "当前速度", Margin = new Padding(12, 0, 0, 0) };
            _elapsedMetric = new MetricView { Dock = DockStyle.Fill, Caption = "已用时间", Margin = new Padding(0) };
            _etaMetric = new MetricView { Dock = DockStyle.Fill, Caption = "预计剩余", Margin = new Padding(12, 0, 0, 0) };
            metrics.Controls.Add(_writtenMetric, 0, 0);
            metrics.Controls.Add(_speedMetric, 1, 0);
            metrics.Controls.Add(_elapsedMetric, 0, 1);
            metrics.Controls.Add(_etaMetric, 1, 1);

            progressTable.Controls.Add(ringHost, 0, 0);
            progressTable.Controls.Add(metrics, 1, 0);

            var capacityPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            var capacityLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            capacityLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            capacityLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            capacityLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var capacityLabels = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            capacityLabels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            capacityLabels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            capacityLabels.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _capacityStartLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Muted,
                Text = "目标空间",
                TextAlign = ContentAlignment.TopLeft,
                Margin = Padding.Empty
            };
            _capacityEndLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Faint,
                Text = "--",
                TextAlign = ContentAlignment.TopRight,
                Margin = Padding.Empty
            };
            _capacityBar = new CapacityBar
            {
                Dock = DockStyle.Fill,
                Height = 10,
                Margin = new Padding(0, 6, 0, 0)
            };
            capacityLabels.Controls.Add(_capacityStartLabel, 0, 0);
            capacityLabels.Controls.Add(_capacityEndLabel, 1, 0);
            capacityLayout.Controls.Add(capacityLabels, 0, 0);
            capacityLayout.Controls.Add(_capacityBar, 0, 1);
            capacityPanel.Controls.Add(capacityLayout);

            var actions = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty
            };
            _pauseButton = new ActionButton
            {
                Style = ButtonStyle.Primary,
                Glyph = ButtonGlyph.Pause,
                Text = "暂停写入",
                Size = new Size(140, 42),
                Location = new Point(0, 5)
            };
            _pauseButton.Click += async delegate { await OnPrimaryActionAsync(); };
            _stopButton = new ActionButton
            {
                Style = ButtonStyle.Secondary,
                Glyph = ButtonGlyph.Stop,
                Text = "停止并保留",
                Size = new Size(148, 42),
                Location = new Point(152, 5)
            };
            _stopButton.Click += async delegate { await OnStopActionAsync(); };
            actions.Controls.Add(_pauseButton);
            actions.Controls.Add(_stopButton);

            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(progressTable, 0, 1);
            layout.Controls.Add(capacityPanel, 0, 2);
            layout.Controls.Add(actions, 0, 3);
            view.Controls.Add(layout);
            return view;
        }

        private Panel CreateSidePanel()
        {
            var side = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty,
                Padding = new Padding(24, 18, 24, 14)
            };
            side.Paint += delegate (object sender, PaintEventArgs e)
            {
                using (var pen = new Pen(AppTheme.Border))
                {
                    e.Graphics.DrawLine(pen, 0, 0, 0, side.Height);
                }
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                ColumnCount = 1,
                RowCount = 12,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            var sideHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty
            };
            var sideTitle = new Label
            {
                AutoSize = true,
                Font = AppTheme.Heading,
                ForeColor = AppTheme.Ink,
                Location = new Point(0, 2),
                Text = "任务配置"
            };
            _sidePill = new StatusPill
            {
                Text = "待开始",
                Tone = PillTone.Neutral,
                Size = new Size(76, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(sideHeader.Width - 76, 0)
            };
            sideHeader.Resize += delegate
            {
                _sidePill.Location = new Point(sideHeader.Width - _sidePill.Width, 0);
            };
            sideHeader.Controls.Add(sideTitle);
            sideHeader.Controls.Add(_sidePill);

            var selectionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty
            };
            _selectedDriveLabel = new Label
            {
                AutoEllipsis = true,
                Font = AppTheme.Heading,
                ForeColor = AppTheme.Ink,
                Location = new Point(0, 0),
                Size = new Size(310, 24),
                Text = "请选择磁盘"
            };
            _selectedMetaLabel = new Label
            {
                AutoEllipsis = true,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Muted,
                Location = new Point(0, 24),
                Size = new Size(310, 18),
                Text = "等待扫描可用磁盘"
            };
            selectionPanel.Controls.Add(_selectedDriveLabel);
            selectionPanel.Controls.Add(_selectedMetaLabel);

            var rule = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 6),
                BackColor = AppTheme.Border
            };

            var details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                ColumnCount = 2,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var row = 0; row < 4; row++)
            {
                details.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            }

            _targetDetailValue = AddDetailRow(details, 0, "目标磁盘");
            _formatDetailValue = AddDetailRow(details, 1, "文件系统");
            _freeDetailValue = AddDetailRow(details, 2, "可写入空间");
            _mediaDetailValue = AddDetailRow(details, 3, "介质类型");

            var modeLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.BodySemibold,
                ForeColor = AppTheme.Ink,
                Text = "写入模式",
                TextAlign = ContentAlignment.BottomLeft,
                Margin = Padding.Empty
            };
            _modeSelector = new ModeSelector
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            _modeSelector.SelectedModeChanged += OnModeChanged;
            _modeDescriptionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Muted,
                Text = "使用确定性非零数据，避免全零文件被压缩。",
                Margin = Padding.Empty
            };
            _deleteAfterCompletionCheckBox = new CheckBox
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Muted,
                Text = "写满后自动删除填充文件",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = Padding.Empty,
                UseVisualStyleBackColor = true
            };
            _deleteAfterCompletionCheckBox.CheckedChanged += delegate
            {
                _deleteAfterCompletion = _deleteAfterCompletionCheckBox.Checked;
            };

            _sideNotice = new NoticeBar
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 2),
                Text = "请选择一块已经格式化、没有需要保留数据的机械硬盘。"
            };

            _cleanupButton = new ActionButton
            {
                Dock = DockStyle.Fill,
                Style = ButtonStyle.Ghost,
                Glyph = ButtonGlyph.Delete,
                Text = "删除填充文件",
                Margin = Padding.Empty,
                Visible = false
            };
            _cleanupButton.Click += OnCleanupButtonClicked;
            _toolTip.SetToolTip(_cleanupButton, "仅删除 DiskFill 在目标盘创建的填充文件。");

            _primaryButton = new ActionButton
            {
                Dock = DockStyle.Fill,
                Style = ButtonStyle.Primary,
                Glyph = ButtonGlyph.Play,
                Text = "开始覆写",
                Margin = Padding.Empty,
                Enabled = false
            };
            _primaryButton.Click += async delegate { await OnPrimaryActionAsync(); };

            layout.Controls.Add(sideHeader, 0, 0);
            layout.Controls.Add(selectionPanel, 0, 1);
            layout.Controls.Add(rule, 0, 2);
            layout.Controls.Add(details, 0, 3);
            layout.Controls.Add(modeLabel, 0, 4);
            layout.Controls.Add(_modeSelector, 0, 5);
            layout.Controls.Add(_modeDescriptionLabel, 0, 6);
            layout.Controls.Add(_deleteAfterCompletionCheckBox, 0, 7);
            layout.Controls.Add(_sideNotice, 0, 8);
            layout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface }, 0, 9);
            layout.Controls.Add(_cleanupButton, 0, 10);
            layout.Controls.Add(_primaryButton, 0, 11);
            side.Controls.Add(layout);
            return side;
        }

        private static Label AddDetailRow(TableLayoutPanel table, int row, string caption)
        {
            var captionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Faint,
                Text = caption,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty
            };
            var valueLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = AppTheme.BodySemibold,
                ForeColor = AppTheme.Ink,
                Text = "--",
                TextAlign = ContentAlignment.MiddleRight,
                Margin = Padding.Empty
            };
            table.Controls.Add(captionLabel, 0, row);
            table.Controls.Add(valueLabel, 1, row);
            return valueLabel;
        }

        private Panel CreateFooter()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Canvas,
                Margin = Padding.Empty,
                Padding = new Padding(28, 0, 28, 0)
            };
            var dot = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.Accent,
                Location = new Point(28, 9),
                Text = "●"
            };
            _footerStatusLabel = new Label
            {
                AutoSize = true,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Muted,
                Location = new Point(44, 8),
                Text = "就绪 · .NET Framework 4.8"
            };
            var logLink = new LinkLabel
            {
                AutoSize = true,
                Font = AppTheme.Small,
                LinkColor = AppTheme.Muted,
                ActiveLinkColor = AppTheme.Accent,
                VisitedLinkColor = AppTheme.Muted,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footer.Width - 144, 8),
                Text = "打开日志"
            };
            logLink.LinkClicked += delegate { OpenLogDirectory(); };
            var version = new Label
            {
                AutoSize = true,
                Font = AppTheme.Small,
                ForeColor = AppTheme.Faint,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footer.Width - 74, 8),
                Text = "v1.0.0"
            };
            footer.Resize += delegate
            {
                logLink.Location = new Point(footer.Width - 146, 8);
                version.Location = new Point(footer.Width - 72, 8);
            };
            footer.Controls.Add(dot);
            footer.Controls.Add(_footerStatusLabel);
            footer.Controls.Add(logLink);
            footer.Controls.Add(version);
            return footer;
        }

        private async Task RefreshDrivesAsync(string preferredRoot)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            _refreshButton.Enabled = false;
            _driveCountLabel.Text = "正在扫描";

            try
            {
                var preferred = preferredRoot ?? (_selectedDrive == null ? null : _selectedDrive.RootPath);
                var snapshot = await Task.Run(() => LoadDriveSnapshot());

                ClearDriveCards();
                foreach (var drive in snapshot.Drives)
                {
                    JobInspection inspection;
                    if (!snapshot.Inspections.TryGetValue(drive.RootPath, out inspection))
                    {
                        inspection = new JobInspection();
                    }

                    var card = new DriveCard(drive);
                    card.SetInspection(inspection);
                    card.Click += OnDriveCardClicked;
                    _driveCards.Add(card);
                    _driveFlow.Controls.Add(card);
                }

                _driveCountLabel.Text = snapshot.Drives.Count == 0
                    ? "未发现本地磁盘"
                    : snapshot.Drives.Count + " 块本地磁盘 · "
                        + snapshot.Drives.Count(drive => drive.IsEligible) + " 块可写入";

                if (snapshot.Drives.Count == 0)
                {
                    AddEmptyDriveState();
                }

                var selectedCard = _driveCards.FirstOrDefault(
                    card => string.Equals(card.Drive.RootPath, preferred, StringComparison.OrdinalIgnoreCase));
                if (selectedCard == null)
                {
                    selectedCard = _driveCards.FirstOrDefault(card => card.Drive.IsEligible)
                        ?? _driveCards.FirstOrDefault();
                }

                SelectDrive(selectedCard);
                AdjustDriveCardWidths();
                _footerStatusLabel.Text = snapshot.Drives.Count == 0
                    ? "未发现可处理的本地磁盘"
                    : "磁盘扫描完成";
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not refresh the drive list.", exception);
                MessageBox.Show(
                    this,
                    "扫描磁盘失败：\r\n" + exception.Message,
                    "DiskFill",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
                _refreshButton.Enabled = _screenState == ScreenState.Idle;
            }
        }

        private DriveSnapshot LoadDriveSnapshot()
        {
            var drives = _driveService.GetDrives();
            var snapshot = new DriveSnapshot();
            foreach (var drive in drives)
            {
                snapshot.Drives.Add(drive);
                snapshot.Inspections[drive.RootPath] = _jobStore.Inspect(drive);
            }

            return snapshot;
        }

        private void ClearDriveCards()
        {
            foreach (var card in _driveCards)
            {
                card.Click -= OnDriveCardClicked;
                _driveFlow.Controls.Remove(card);
                card.Dispose();
            }

            _driveCards.Clear();
            _driveFlow.ClearItems();
        }

        private void AddEmptyDriveState()
        {
            var empty = new Panel
            {
                Width = Math.Max(300, _driveFlow.ClientSize.Width - 20),
                Height = 176,
                BackColor = AppTheme.Canvas,
                Margin = new Padding(0, 8, 0, 0)
            };
            var title = new Label
            {
                AutoSize = false,
                Font = AppTheme.Heading,
                ForeColor = AppTheme.Muted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 52),
                Size = new Size(empty.Width, 34),
                Text = "没有检测到可处理的本地磁盘"
            };
            var detail = new Label
            {
                AutoSize = false,
                Font = AppTheme.Body,
                ForeColor = AppTheme.Faint,
                TextAlign = ContentAlignment.TopCenter,
                Location = new Point(0, 91),
                Size = new Size(empty.Width, 50),
                Text = "请连接机械硬盘并确认卷已经挂载，然后点击右上角刷新。"
            };
            empty.Resize += delegate
            {
                title.Width = empty.Width;
                detail.Width = empty.Width;
            };
            empty.Controls.Add(title);
            empty.Controls.Add(detail);
            _driveFlow.Controls.Add(empty);
            _driveFlow.RelayoutItems();
        }

        private void AdjustDriveCardWidths()
        {
            _driveFlow.RelayoutItems();
        }

        private void OnDriveCardClicked(object sender, EventArgs e)
        {
            if (_screenState != ScreenState.Idle || _isBusy)
            {
                return;
            }

            SelectDrive(sender as DriveCard);
        }

        private void SelectDrive(DriveCard card)
        {
            foreach (var driveCard in _driveCards)
            {
                driveCard.Selected = driveCard == card;
            }

            _selectedDrive = card == null ? null : card.Drive;
            _selectedInspection = card == null ? null : card.Inspection;
            UpdateSidePanelForSelection();
        }

        private void UpdateSidePanelForSelection()
        {
            if (_selectedDrive == null)
            {
                _selectedDriveLabel.Text = "请选择磁盘";
                _selectedMetaLabel.Text = "等待扫描可用磁盘";
                _targetDetailValue.Text = "--";
                _formatDetailValue.Text = "--";
                _freeDetailValue.Text = "--";
                _mediaDetailValue.Text = "--";
                _sidePill.Text = "待开始";
                _sidePill.Tone = PillTone.Neutral;
                _sideNotice.Tone = PillTone.Warning;
                _sideNotice.Text = "请选择一块已经格式化、没有需要保留数据的机械硬盘。";
                _primaryButton.Enabled = false;
                _cleanupButton.Visible = false;
                return;
            }

            _selectedDriveLabel.Text = _selectedDrive.DisplayName + "  " + _selectedDrive.Label;
            _selectedMetaLabel.Text = _selectedDrive.FileSystem + "  ·  "
                + ByteFormatter.Format(_selectedDrive.TotalBytes) + "  ·  "
                + _selectedDrive.MediaDescription;
            _targetDetailValue.Text = _selectedDrive.DisplayName + "  " + _selectedDrive.Label;
            _formatDetailValue.Text = _selectedDrive.FileSystem + "  ·  "
                + ByteFormatter.Format(_selectedDrive.TotalBytes);
            _freeDetailValue.Text = ByteFormatter.Format(_selectedDrive.AvailableBytes);
            _mediaDetailValue.Text = _selectedDrive.MediaDescription;

            var job = _selectedInspection == null ? null : _selectedInspection.Job;
            var hasIssue = _selectedInspection != null && _selectedInspection.HasIssue;
            _suppressModeChange = true;
            _selectedMode = job == null ? _selectedMode : job.Mode;
            _modeSelector.SelectedMode = _selectedMode;
            _suppressModeChange = false;
            UpdateModeDescription();

            if (hasIssue)
            {
                _sidePill.Text = "需检查";
                _sidePill.Tone = PillTone.Danger;
                _sideNotice.Tone = PillTone.Danger;
                _sideNotice.Text = _selectedInspection.Message;
                _primaryButton.Text = "无法开始";
                _primaryButton.Glyph = ButtonGlyph.None;
                _primaryButton.Style = ButtonStyle.Secondary;
                _primaryButton.Enabled = false;
                _cleanupButton.Visible = false;
            }
            else if (job != null)
            {
                if (job.State == FillJobState.Completed)
                {
                    _sidePill.Text = "待清理";
                    _sidePill.Tone = PillTone.Accent;
                    _sideNotice.Tone = PillTone.Accent;
                    _sideNotice.Text = "该任务已经完成，填充文件仍保留在目标盘。删除文件可释放空间，已完成的覆写不会撤销。";
                    _primaryButton.Text = "删除填充文件";
                    _primaryButton.Glyph = ButtonGlyph.Delete;
                    _primaryButton.Style = ButtonStyle.Secondary;
                    _primaryButton.Enabled = true;
                    _cleanupButton.Visible = false;
                }
                else
                {
                    _sidePill.Text = "可续写";
                    _sidePill.Tone = PillTone.Accent;
                    _sideNotice.Tone = PillTone.Accent;
                    _sideNotice.Text = "检测到未完成任务。继续后将从已写入位置顺序追加，不会清空已有进度。";
                    _primaryButton.Text = "继续覆写";
                    _primaryButton.Glyph = ButtonGlyph.Play;
                    _primaryButton.Style = ButtonStyle.Primary;
                    _primaryButton.Enabled = _selectedDrive.IsEligible;
                    _cleanupButton.Visible = true;
                    _cleanupButton.Enabled = true;
                }
            }
            else if (!_selectedDrive.IsEligible)
            {
                _sidePill.Text = "受保护";
                _sidePill.Tone = PillTone.Warning;
                _sideNotice.Tone = PillTone.Warning;
                _sideNotice.Text = _selectedDrive.IneligibleReason
                    + "。DiskFill 只会写入普通的可用簇，不能替代固态硬盘的安全擦除。";
                _primaryButton.Text = "不可开始";
                _primaryButton.Glyph = ButtonGlyph.None;
                _primaryButton.Style = ButtonStyle.Secondary;
                _primaryButton.Enabled = false;
                _cleanupButton.Visible = false;
            }
            else
            {
                _sidePill.Text = _selectedDrive.MediaKind == MediaKind.Unknown ? "需确认" : "待开始";
                _sidePill.Tone = _selectedDrive.MediaKind == MediaKind.Unknown
                    ? PillTone.Warning
                    : PillTone.Neutral;
                _sideNotice.Tone = PillTone.Warning;
                _sideNotice.Text = _selectedDrive.MediaKind == MediaKind.Unknown
                    ? "无法确认该卷是否使用旋转介质。若这是固态硬盘，请取消并使用厂商安全擦除工具。"
                    : "开始后会持续写入，直到该卷的可用空间耗尽。请先确认盘内没有需要保留的数据。";
                _primaryButton.Text = "开始覆写";
                _primaryButton.Glyph = ButtonGlyph.Play;
                _primaryButton.Style = ButtonStyle.Primary;
                _primaryButton.Enabled = true;
                _cleanupButton.Visible = false;
            }

            _modeSelector.Enabled = job == null && _selectedDrive.IsEligible;
            _deleteAfterCompletionCheckBox.Enabled = job == null;
        }

        private void OnModeChanged(object sender, EventArgs e)
        {
            if (_suppressModeChange)
            {
                return;
            }

            _selectedMode = _modeSelector.SelectedMode;
            UpdateModeDescription();
        }

        private void UpdateModeDescription()
        {
            _modeDescriptionLabel.Text = _selectedMode == OverwriteMode.FastNonZero
                ? "使用确定性非零数据，避免全零文件被压缩。"
                : "写入全零数据。通常略快，但压缩卷可能无法充分占满。";
        }

        private async Task OnPrimaryActionAsync()
        {
            switch (_screenState)
            {
                case ScreenState.Running:
                    PauseOperation();
                    break;
                case ScreenState.Paused:
                    await ResumeOrContinueAsync();
                    break;
                case ScreenState.Completed:
                    await DeleteActiveJobAsync(false);
                    break;
                case ScreenState.Failed:
                    await ResumeOrContinueAsync();
                    break;
                default:
                    var existingJob = _selectedInspection == null ? null : _selectedInspection.Job;
                    if (existingJob != null && existingJob.State == FillJobState.Completed)
                    {
                        _activeJob = existingJob;
                        await DeleteActiveJobAsync(false);
                    }
                    else
                    {
                        await StartSelectedJobAsync();
                    }

                    break;
            }
        }

        private async Task OnStopActionAsync()
        {
            if (_isEngineActive)
            {
                await StopOperationAsync();
                return;
            }

            if (_screenState == ScreenState.Completed)
            {
                await ReturnToDriveListAsync();
                return;
            }

            await ReturnToDriveListAsync();
        }

        private async Task StartSelectedJobAsync()
        {
            if (_selectedDrive == null || _isBusy)
            {
                return;
            }

            if (_selectedInspection != null && _selectedInspection.HasIssue)
            {
                MessageBox.Show(
                    this,
                    _selectedInspection.Message,
                    "任务需要检查",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_selectedInspection != null && _selectedInspection.HasJob)
            {
                var job = _selectedInspection.Job;
                if (!await ValidateDriveBeforeResumeAsync(job))
                {
                    await RefreshDrivesAsync(null);
                    return;
                }

                await StartFillAsync(job);
                return;
            }

            if (!_selectedDrive.IsEligible)
            {
                return;
            }

            using (var dialog = new ConfirmFillDialog(_selectedDrive))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            _isBusy = true;
            _primaryButton.Enabled = false;
            _cleanupButton.Enabled = false;
            try
            {
                var freshDrive = await Task.Run(() => _driveService.Refresh(_selectedDrive.RootPath));
                if (freshDrive == null
                    || !freshDrive.IsEligible
                    || !string.Equals(
                        freshDrive.VolumeSerial,
                        _selectedDrive.VolumeSerial,
                        StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        this,
                        "磁盘状态已经变化。请刷新后重新选择。",
                        "DiskFill",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (freshDrive.AvailableBytes < 64L * 1024 * 1024)
                {
                    MessageBox.Show(
                        this,
                        "该磁盘当前可用空间不足 64 MB，无法开始任务。",
                        "DiskFill",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var job = await Task.Run(() => _jobStore.CreateNew(freshDrive, _selectedMode));
                _selectedDrive = freshDrive;
                _selectedInspection = new JobInspection { Job = job };
                await StartFillAsync(job);
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not create a fill job.", exception);
                MessageBox.Show(
                    this,
                    "无法创建覆写任务：\r\n" + exception.Message,
                    "DiskFill",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
                if (_screenState == ScreenState.Idle)
                {
                    UpdateSidePanelForSelection();
                }
            }
        }

        private async Task StartFillAsync(FillJob job)
        {
            if (job == null || _isEngineActive)
            {
                return;
            }

            _activeJob = job;
            _selectedMode = job.Mode;
            _runCancellation = new CancellationTokenSource();
            _pauseController = new PauseController();
            _screenState = ScreenState.Running;
            _isEngineActive = true;
            BeginExecutionState();
            ShowRunView();
            ApplyRunningState(job);

            var progress = new Progress<FillProgress>(OnFillProgress);
            FillResult result;
            try
            {
                result = await _fillEngine.RunAsync(
                    job,
                    progress,
                    _pauseController,
                    _runCancellation.Token);
            }
            catch (Exception exception)
            {
                AppLog.Error("The fill engine ended unexpectedly.", exception);
                result = new FillResult
                {
                    CompletionKind = FillCompletionKind.Failed,
                    WrittenBytes = _jobStore.GetWrittenBytes(job),
                    ErrorMessage = exception.Message
                };
            }

            EndExecutionState();
            _isEngineActive = false;
            _screenState = result.IsSuccess
                ? ScreenState.Completed
                : (result.CompletionKind == FillCompletionKind.Cancelled
                    ? ScreenState.Paused
                    : ScreenState.Failed);

            if (result.IsSuccess)
            {
                ShowCompletedState(result);
                if (_deleteAfterCompletion)
                {
                    await DeleteActiveJobAsync(true);
                }
            }
            else if (result.CompletionKind == FillCompletionKind.Cancelled)
            {
                ShowPausedState();
            }
            else
            {
                ShowFailedState(result);
            }

            CleanupRunObjects();
            if (_closeAfterStop)
            {
                _closeAfterStop = false;
                Close();
            }
        }

        private async Task<bool> ValidateDriveBeforeResumeAsync(FillJob job)
        {
            if (job == null || _selectedDrive == null || _isBusy)
            {
                return false;
            }

            _isBusy = true;
            _primaryButton.Enabled = false;
            try
            {
                var expectedRoot = _selectedDrive.RootPath;
                var freshDrive = await Task.Run(() => _driveService.Refresh(expectedRoot));
                if (freshDrive == null || !job.IsCompatibleWith(freshDrive))
                {
                    MessageBox.Show(
                        this,
                        "目标卷的序列号或容量已经变化。为保护数据，任务不会继续。",
                        "磁盘身份已变化",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                _selectedDrive = freshDrive;
                return true;
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not validate the target drive before resume.", exception);
                MessageBox.Show(
                    this,
                    "无法重新验证目标磁盘：\r\n" + exception.Message,
                    "DiskFill",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                _isBusy = false;
                if (_screenState == ScreenState.Idle)
                {
                    UpdateSidePanelForSelection();
                }
            }
        }

        private void ApplyRunningState(FillJob job)
        {
            _idleView.Visible = false;
            _runView.Visible = true;
            _runView.BringToFront();
            _runTitleLabel.Text = "正在覆写 " + job.VolumeRoot;
            _runSubtitleLabel.Text = "顺序写入中 · "
                + (job.Mode == OverwriteMode.FastNonZero ? "非零填充" : "全零填充")
                + " · " + ByteFormatter.Format(job.PlannedBytes) + " 计划写入量";
            _runPill.Text = "运行中";
            _runPill.Tone = PillTone.Accent;
            _ringProgress.ProgressColor = AppTheme.Accent;
            _ringProgress.Value = job.PlannedBytes <= 0
                ? 0
                : (double)job.WrittenBytes / job.PlannedBytes;
            _capacityBar.ProgressColor = AppTheme.Accent;
            _capacityBar.Value = _ringProgress.Value;
            _capacityEndLabel.Text = ByteFormatter.Format(job.PlannedBytes);
            _writtenMetric.Value = ByteFormatter.Format(job.WrittenBytes);
            _writtenMetric.Note = "计划 " + ByteFormatter.Format(job.PlannedBytes);
            _speedMetric.Value = "--";
            _speedMetric.Note = "顺序写入";
            _elapsedMetric.Value = "00:00";
            _elapsedMetric.Note = "本机计时";
            _etaMetric.Value = "--";
            _etaMetric.Note = "根据当前速度估算";
            _pauseButton.Text = "暂停写入";
            _pauseButton.Glyph = ButtonGlyph.Pause;
            _pauseButton.Enabled = true;
            _stopButton.Text = "停止并保留";
            _stopButton.Glyph = ButtonGlyph.Stop;
            _stopButton.Enabled = true;
            _refreshButton.Enabled = false;
            _modeSelector.Enabled = false;
            _deleteAfterCompletionCheckBox.Enabled = false;
            _sidePill.Text = "运行中";
            _sidePill.Tone = PillTone.Accent;
            _primaryButton.Text = "暂停写入";
            _primaryButton.Glyph = ButtonGlyph.Pause;
            _primaryButton.Style = ButtonStyle.Primary;
            _primaryButton.Enabled = true;
            _cleanupButton.Visible = false;
            _sideNotice.Tone = PillTone.Accent;
            _sideNotice.Text = "任务运行时会阻止系统自动睡眠。请保持磁盘供电稳定，不要强制断电。";
        }

        private void ShowRunView()
        {
            _idleView.Visible = false;
            _runView.Visible = true;
            _runView.BringToFront();
        }

        private void OnFillProgress(FillProgress progress)
        {
            if (progress == null || _activeJob == null)
            {
                return;
            }

            _activeJob.WrittenBytes = progress.WrittenBytes;
            var fraction = Math.Max(0, Math.Min(1, progress.Fraction));
            _ringProgress.Value = fraction;
            _capacityBar.Value = fraction;
            _writtenMetric.Value = ByteFormatter.Format(progress.WrittenBytes);
            _writtenMetric.Note = "计划 " + ByteFormatter.Format(progress.TotalBytes);
            _speedMetric.Value = ByteFormatter.FormatRate(progress.BytesPerSecond);
            _speedMetric.Note = "4 MiB 顺序写入";
            var elapsed = TimeSpan.FromSeconds(progress.ElapsedSeconds);
            _elapsedMetric.Value = TimeFormatter.FormatElapsed(elapsed);
            _elapsedMetric.Note = "本次任务";
            _capacityEndLabel.Text = ByteFormatter.Format(progress.WrittenBytes)
                + " / " + ByteFormatter.Format(progress.TotalBytes);

            if (progress.BytesPerSecond > 0 && progress.WrittenBytes < progress.TotalBytes)
            {
                var remainingSeconds = (progress.TotalBytes - progress.WrittenBytes)
                    / progress.BytesPerSecond;
                _etaMetric.Value = TimeFormatter.FormatEta(TimeSpan.FromSeconds(remainingSeconds));
            }
            else
            {
                _etaMetric.Value = "--";
            }
        }

        private void PauseOperation()
        {
            if (_screenState != ScreenState.Running || _pauseController == null)
            {
                return;
            }

            _pauseController.Pause();
            EndExecutionState();
            _screenState = ScreenState.Paused;
            if (_activeJob != null)
            {
                _activeJob.State = FillJobState.Paused;
                var job = _activeJob;
                Task.Run(() => _jobStore.SaveQuietly(job));
            }

            ShowPausedState();
        }

        private async Task ResumeOrContinueAsync()
        {
            if (_activeJob == null)
            {
                return;
            }

            if (_screenState == ScreenState.Paused && _isEngineActive && _pauseController != null)
            {
                _pauseController.Resume();
                BeginExecutionState();
                _activeJob.State = FillJobState.Running;
                _screenState = ScreenState.Running;
                ApplyRunningState(_activeJob);
                return;
            }

            await StartFillAsync(_activeJob);
        }

        private async Task StopOperationAsync()
        {
            if (!_isEngineActive || _runCancellation == null)
            {
                return;
            }

            var answer = MessageBox.Show(
                this,
                "停止当前任务并保留已写入的填充文件？\r\n\r\n之后可以从磁盘列表继续写入。",
                "停止覆写",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            _stopButton.Enabled = false;
            _pauseButton.Enabled = false;
            _primaryButton.Enabled = false;
            if (_pauseController != null)
            {
                _pauseController.Resume();
            }

            _runCancellation.Cancel();
        }

        private void ShowPausedState()
        {
            _screenState = ScreenState.Paused;
            _runView.Visible = true;
            _idleView.Visible = false;
            _runTitleLabel.Text = "任务已暂停";
            _runSubtitleLabel.Text = "已写入进度已经保留，继续后会从当前文件末尾追加。";
            _runPill.Text = "已暂停";
            _runPill.Tone = PillTone.Warning;
            _ringProgress.ProgressColor = AppTheme.Warning;
            _capacityBar.ProgressColor = AppTheme.Warning;
            _pauseButton.Text = "继续写入";
            _pauseButton.Glyph = ButtonGlyph.Play;
            _pauseButton.Style = ButtonStyle.Primary;
            _pauseButton.Enabled = _activeJob != null;
            _stopButton.Text = _isEngineActive ? "停止并保留" : "返回磁盘列表";
            _stopButton.Glyph = _isEngineActive ? ButtonGlyph.Stop : ButtonGlyph.None;
            _stopButton.Enabled = true;
            _refreshButton.Enabled = false;
            _modeSelector.Enabled = false;
            _deleteAfterCompletionCheckBox.Enabled = false;
            _sidePill.Text = "已暂停";
            _sidePill.Tone = PillTone.Warning;
            _primaryButton.Text = "继续写入";
            _primaryButton.Glyph = ButtonGlyph.Play;
            _primaryButton.Style = ButtonStyle.Primary;
            _primaryButton.Enabled = _activeJob != null;
            _cleanupButton.Text = "删除填充文件";
            _cleanupButton.Glyph = ButtonGlyph.Delete;
            _cleanupButton.Visible = _activeJob != null && !_isEngineActive;
            _cleanupButton.Enabled = _activeJob != null && !_isEngineActive;
            _sideNotice.Tone = PillTone.Warning;
            _sideNotice.Text = "任务处于暂停状态。填充文件不会被删除，可以安全退出程序并在下次继续。";
            _footerStatusLabel.Text = "任务已暂停 · 进度已保留";
        }

        private void ShowCompletedState(FillResult result)
        {
            _screenState = ScreenState.Completed;
            _runView.Visible = true;
            _idleView.Visible = false;
            _runTitleLabel.Text = "覆写完成";
            _runSubtitleLabel.Text = "已顺序写入 " + ByteFormatter.Format(result.WrittenBytes)
                + "，目标卷可用空间已耗尽。";
            _runPill.Text = "已完成";
            _runPill.Tone = PillTone.Accent;
            _ringProgress.Value = 1;
            _ringProgress.ProgressColor = AppTheme.Accent;
            _capacityBar.Value = 1;
            _capacityBar.ProgressColor = AppTheme.Accent;
            _capacityEndLabel.Text = ByteFormatter.Format(result.WrittenBytes);
            _writtenMetric.Value = ByteFormatter.Format(result.WrittenBytes);
            _writtenMetric.Note = "最终写入量";
            var averageRate = result.Elapsed.TotalSeconds > 0
                ? result.WrittenBytes / result.Elapsed.TotalSeconds
                : 0;
            _speedMetric.Value = ByteFormatter.FormatRate(averageRate);
            _speedMetric.Note = "平均速度";
            _elapsedMetric.Value = TimeFormatter.FormatElapsed(result.Elapsed);
            _elapsedMetric.Note = "总耗时";
            _etaMetric.Value = "0 分钟";
            _etaMetric.Note = "写入已完成";
            _pauseButton.Text = "删除填充文件";
            _pauseButton.Glyph = ButtonGlyph.Delete;
            _pauseButton.Style = ButtonStyle.Secondary;
            _pauseButton.Enabled = _activeJob != null;
            _stopButton.Text = "返回磁盘列表";
            _stopButton.Glyph = ButtonGlyph.None;
            _stopButton.Style = ButtonStyle.Secondary;
            _stopButton.Enabled = true;
            _refreshButton.Enabled = false;
            _modeSelector.Enabled = false;
            _deleteAfterCompletionCheckBox.Enabled = false;
            _sidePill.Text = "已完成";
            _sidePill.Tone = PillTone.Accent;
            _primaryButton.Text = "删除填充文件";
            _primaryButton.Glyph = ButtonGlyph.Delete;
            _primaryButton.Style = ButtonStyle.Secondary;
            _primaryButton.Enabled = _activeJob != null;
            _cleanupButton.Text = "返回磁盘列表";
            _cleanupButton.Glyph = ButtonGlyph.None;
            _cleanupButton.Visible = true;
            _cleanupButton.Enabled = true;
            _sideNotice.Tone = PillTone.Accent;
            _sideNotice.Text = "填充文件仍然保留。删除它只会释放空间，不会撤销已经完成的数据簇覆写。";
            _freeDetailValue.Text = "已写满";
            _footerStatusLabel.Text = "覆写完成 · " + ByteFormatter.Format(result.WrittenBytes) + " 已写入";
            AppLog.Info("Fill operation completed in the UI.");
        }

        private void ShowFailedState(FillResult result)
        {
            _screenState = ScreenState.Failed;
            _runView.Visible = true;
            _idleView.Visible = false;
            _runTitleLabel.Text = "任务中断";
            _runSubtitleLabel.Text = string.IsNullOrEmpty(result.ErrorMessage)
                ? "写入过程遇到错误。"
                : result.ErrorMessage;
            _runPill.Text = "需处理";
            _runPill.Tone = PillTone.Danger;
            _ringProgress.ProgressColor = AppTheme.Danger;
            _capacityBar.ProgressColor = AppTheme.Danger;
            _writtenMetric.Value = ByteFormatter.Format(result.WrittenBytes);
            _writtenMetric.Note = "已保存的写入量";
            _pauseButton.Text = "重试续写";
            _pauseButton.Glyph = ButtonGlyph.Play;
            _pauseButton.Style = ButtonStyle.Primary;
            _pauseButton.Enabled = _activeJob != null;
            _stopButton.Text = "返回磁盘列表";
            _stopButton.Glyph = ButtonGlyph.None;
            _stopButton.Style = ButtonStyle.Secondary;
            _stopButton.Enabled = true;
            _refreshButton.Enabled = false;
            _modeSelector.Enabled = false;
            _deleteAfterCompletionCheckBox.Enabled = false;
            _sidePill.Text = "需处理";
            _sidePill.Tone = PillTone.Danger;
            _primaryButton.Text = "重试续写";
            _primaryButton.Glyph = ButtonGlyph.Play;
            _primaryButton.Style = ButtonStyle.Primary;
            _primaryButton.Enabled = _activeJob != null;
            _cleanupButton.Text = "删除填充文件";
            _cleanupButton.Glyph = ButtonGlyph.Delete;
            _cleanupButton.Visible = _activeJob != null;
            _cleanupButton.Enabled = _activeJob != null;
            _sideNotice.Tone = PillTone.Danger;
            _sideNotice.Text = "写入已停止。可以检查磁盘连接、空间或错误日志后重试；已写入进度会保留。";
            _footerStatusLabel.Text = "任务中断 · 可查看日志后重试";
        }

        private async void OnCleanupButtonClicked(object sender, EventArgs e)
        {
            if (_screenState == ScreenState.Completed)
            {
                await ReturnToDriveListAsync();
                return;
            }

            await DeleteActiveJobAsync(false);
        }

        private async Task ReturnToDriveListAsync()
        {
            if (_isEngineActive)
            {
                return;
            }

            _activeJob = null;
            _selectedInspection = null;
            _screenState = ScreenState.Idle;
            _runView.Visible = false;
            _idleView.Visible = true;
            _idleView.BringToFront();
            _cleanupButton.Text = "删除填充文件";
            _cleanupButton.Glyph = ButtonGlyph.Delete;
            _pauseButton.Style = ButtonStyle.Primary;
            await RefreshDrivesAsync(_selectedDrive == null ? null : _selectedDrive.RootPath);
        }

        private async Task DeleteActiveJobAsync(bool silent)
        {
            if (_activeJob == null)
            {
                return;
            }

            if (_isEngineActive)
            {
                return;
            }

            if (!silent)
            {
                var answer = MessageBox.Show(
                    this,
                    "删除 DiskFill 创建的全部填充文件并释放空间？\r\n\r\n已完成的文件级覆写不会撤销。",
                    "删除填充文件",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            _primaryButton.Enabled = false;
            _cleanupButton.Enabled = false;
            _pauseButton.Enabled = false;
            _stopButton.Enabled = false;

            var job = _activeJob;
            try
            {
                await Task.Run(() => _jobStore.DeleteJob(job));
                _activeJob = null;
                _selectedInspection = null;
                _screenState = ScreenState.Idle;
                _runView.Visible = false;
                _idleView.Visible = true;
                _idleView.BringToFront();
                _footerStatusLabel.Text = "填充文件已删除 · 磁盘空间已释放";
                _cleanupButton.Text = "删除填充文件";
                _cleanupButton.Glyph = ButtonGlyph.Delete;
                await RefreshDrivesAsync(job.VolumeRoot);
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not delete the fill files.", exception);
                MessageBox.Show(
                    this,
                    "删除填充文件失败：\r\n" + exception.Message,
                    "DiskFill",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _screenState = ScreenState.Completed;
                _primaryButton.Enabled = _activeJob != null;
                _cleanupButton.Enabled = true;
                _pauseButton.Enabled = _activeJob != null;
                _stopButton.Enabled = true;
            }
        }

        private void CleanupRunObjects()
        {
            EndExecutionState();
            if (_runCancellation != null)
            {
                _runCancellation.Dispose();
                _runCancellation = null;
            }

            if (_pauseController != null)
            {
                _pauseController.Dispose();
                _pauseController = null;
            }
        }

        private void BeginExecutionState()
        {
            if (_executionStateScope == null)
            {
                _executionStateScope = new ExecutionStateScope();
            }
        }

        private void EndExecutionState()
        {
            if (_executionStateScope == null)
            {
                return;
            }

            _executionStateScope.Dispose();
            _executionStateScope = null;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isEngineActive && _runCancellation != null)
            {
                var answer = MessageBox.Show(
                    this,
                    "任务仍在运行。退出会先停止任务，已写入内容会保留，下次可以继续。\r\n\r\n确定退出？",
                    "退出 DiskFill",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                e.Cancel = true;
                _closeAfterStop = true;
                if (_pauseController != null)
                {
                    _pauseController.Resume();
                }

                _runCancellation.Cancel();
                _primaryButton.Enabled = false;
                _cleanupButton.Enabled = false;
                _pauseButton.Enabled = false;
                _stopButton.Enabled = false;
            }
        }

        private static void OpenLogDirectory()
        {
            try
            {
                Directory.CreateDirectory(AppLog.LogDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppLog.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not open the log directory.", exception);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                EndExecutionState();
                _toolTip.Dispose();
                if (_runCancellation != null)
                {
                    _runCancellation.Dispose();
                }

                if (_pauseController != null)
                {
                    _pauseController.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private sealed class DriveSnapshot
        {
            public DriveSnapshot()
            {
                Drives = new List<DriveTarget>();
                Inspections = new Dictionary<string, JobInspection>(StringComparer.OrdinalIgnoreCase);
            }

            public List<DriveTarget> Drives { get; private set; }

            public Dictionary<string, JobInspection> Inspections { get; private set; }
        }
    }
}
