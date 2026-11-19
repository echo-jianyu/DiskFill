using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DiskFill.UI.Controls
{
    internal sealed class ScrollableStackPanel : Panel
    {
        private const int SbHorizontal = 0;

        private int _lastLayoutWidth = -1;
        private int _lastLayoutHeight = -1;
        private bool _isRelayouting;

        public ScrollableStackPanel()
        {
            AutoScroll = true;
            BackColor = AppTheme.Canvas;
            Padding = new Padding(0, 2, 0, 4);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
        }

        public void ClearItems()
        {
            SuspendLayout();
            try
            {
                while (Controls.Count > 0)
                {
                    var control = Controls[0];
                    Controls.RemoveAt(0);
                    control.Dispose();
                }
            }
            finally
            {
                ResumeLayout(true);
            }

            RelayoutItems();
        }

        public void RelayoutItems()
        {
            if (IsDisposed || Disposing || _isRelayouting)
            {
                return;
            }

            _isRelayouting = true;
            SuspendLayout();
            var autoScroll = AutoScroll;
            AutoScroll = false;
            try
            {
                var totalHeight = Padding.Top;
                foreach (Control control in Controls)
                {
                    if (!control.Visible)
                    {
                        continue;
                    }

                    totalHeight += control.Margin.Vertical + control.Height;
                }

                totalHeight += Padding.Bottom;
                var verticalScrollbar = totalHeight > ClientSize.Height
                    ? SystemInformation.VerticalScrollBarWidth
                    : 0;
                var itemWidth = Math.Max(
                    280,
                    ClientSize.Width - Padding.Horizontal - verticalScrollbar);
                AutoScrollMinSize = new Size(0, totalHeight);

                var top = Padding.Top;
                foreach (Control control in Controls)
                {
                    if (!control.Visible)
                    {
                        continue;
                    }

                    top += control.Margin.Top;
                    control.Left = Padding.Left;
                    control.Top = top;
                    control.Width = itemWidth;
                    top += control.Height + control.Margin.Bottom;
                }

                _lastLayoutWidth = ClientSize.Width;
                _lastLayoutHeight = ClientSize.Height;
            }
            finally
            {
                AutoScroll = autoScroll;
                _isRelayouting = false;
                ResumeLayout(true);
                // WinForms keeps a stale horizontal scroll extent after cards shrink.
                HorizontalScroll.Maximum = 0;
                HorizontalScroll.Visible = false;
                HideHorizontalScrollbar();
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HideHorizontalScrollbar();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_lastLayoutWidth != ClientSize.Width || _lastLayoutHeight != ClientSize.Height)
            {
                RelayoutItems();
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            RelayoutItems();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            RelayoutItems();
        }

        private void HideHorizontalScrollbar()
        {
            if (IsHandleCreated)
            {
                ShowScrollBar(Handle, SbHorizontal, false);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowScrollBar(IntPtr windowHandle, int scrollBar, bool show);
    }
}
