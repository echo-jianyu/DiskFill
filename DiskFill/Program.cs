using System;
using System.Threading;
using System.Windows.Forms;
using DiskFill.Core.Infrastructure;
using DiskFill.UI;

namespace DiskFill
{
    internal static class Program
    {
        private const string MutexName = "DiskFill.Desktop.SingleInstance";

        [STAThread]
        private static void Main()
        {
            using (var mutex = new Mutex(true, MutexName, out bool isFirstInstance))
            {
                if (!isFirstInstance)
                {
                    MessageBox.Show(
                        "DiskFill 已经在运行。请切换到现有窗口后重试。",
                        "DiskFill",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                try
                {
                    Application.Run(new MainForm());
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLog.Error("Unhandled UI exception.", e.Exception);
            MessageBox.Show(
                "程序遇到未处理的错误。日志已写入本机，请检查后重试。\r\n\r\n" + e.Exception.Message,
                "DiskFill 错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppLog.Error("Unhandled process exception.", e.ExceptionObject as Exception);
        }
    }
}
