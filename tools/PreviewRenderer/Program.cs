using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace DiskFill.PreviewRenderer
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var outputDirectory = args.Length > 0
                ? Path.GetFullPath(args[0])
                : Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
            Directory.CreateDirectory(outputDirectory);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var assemblyPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "DiskFill.exe");
            var assembly = Assembly.LoadFrom(assemblyPath);
            var mainFormType = assembly.GetType("DiskFill.UI.MainForm", true);
            var form = (Form)Activator.CreateInstance(mainFormType, true);
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);

            try
            {
                form.Show();
                PumpMessages(TimeSpan.FromSeconds(2.2));
                Capture(form, Path.Combine(outputDirectory, "ui-main.png"));
                form.Size = new Size(1040, 720);
                PumpMessages(TimeSpan.FromMilliseconds(300));
                Capture(form, Path.Combine(outputDirectory, "ui-main-small.png"));
                form.Size = new Size(1180, 780);
                PumpMessages(TimeSpan.FromMilliseconds(300));

                var driveServiceType = assembly.GetType("DiskFill.Core.Services.DriveService", true);
                var driveService = Activator.CreateInstance(driveServiceType);
                var drives = (IEnumerable)driveServiceType
                    .GetMethod("GetDrives")
                    .Invoke(driveService, null);
                var drive = drives.Cast<object>()
                    .FirstOrDefault(item => (bool)item.GetType()
                        .GetProperty("IsEligible")
                        .GetValue(item, null));
                if (drive == null)
                {
                    drive = drives.Cast<object>().FirstOrDefault();
                }

                if (drive != null)
                {
                    var fillJobType = assembly.GetType("DiskFill.Core.Models.FillJob", true);
                    var fillJob = Activator.CreateInstance(fillJobType);
                    var totalBytes = (long)drive.GetType()
                        .GetProperty("AvailableBytes")
                        .GetValue(drive, null);
                    fillJobType.GetProperty("JobId").SetValue(fillJob, "preview", null);
                    fillJobType.GetProperty("VolumeRoot").SetValue(
                        fillJob,
                        drive.GetType().GetProperty("RootPath").GetValue(drive, null),
                        null);
                    fillJobType.GetProperty("Mode").SetValue(fillJob, Enum.ToObject(
                        assembly.GetType("DiskFill.Core.Models.OverwriteMode", true),
                        0), null);
                    fillJobType.GetProperty("PlannedBytes").SetValue(fillJob, totalBytes, null);
                    fillJobType.GetProperty("WrittenBytes").SetValue(
                        fillJob,
                        (long)(totalBytes * 0.42D),
                        null);
                    mainFormType.GetField(
                        "_activeJob",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(form, fillJob);
                    mainFormType.GetMethod(
                        "ApplyRunningState",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { fillJob });

                    var progressType = assembly.GetType("DiskFill.Core.Models.FillProgress", true);
                    var progress = Activator.CreateInstance(progressType);
                    progressType.GetProperty("WrittenBytes").SetValue(
                        progress,
                        (long)(totalBytes * 0.42D),
                        null);
                    progressType.GetProperty("TotalBytes").SetValue(progress, totalBytes, null);
                    progressType.GetProperty("BytesPerSecond").SetValue(
                        progress,
                        148D * 1024D * 1024D,
                        null);
                    progressType.GetProperty("ElapsedSeconds").SetValue(progress, 1864D, null);
                    mainFormType.GetMethod(
                        "OnFillProgress",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { progress });
                    PumpMessages(TimeSpan.FromMilliseconds(250));
                    Capture(form, Path.Combine(outputDirectory, "ui-running.png"));

                    var resultType = assembly.GetType("DiskFill.Core.Models.FillResult", true);
                    var result = Activator.CreateInstance(resultType);
                    resultType.GetProperty("WrittenBytes").SetValue(
                        result,
                        totalBytes - 2L * 1024 * 1024,
                        null);
                    resultType.GetProperty("Elapsed").SetValue(
                        result,
                        TimeSpan.FromHours(1.37D),
                        null);
                    resultType.GetProperty("CompletionKind").SetValue(
                        result,
                        Enum.ToObject(
                            assembly.GetType("DiskFill.Core.Models.FillCompletionKind", true),
                            0),
                        null);
                    mainFormType.GetMethod(
                        "ShowCompletedState",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { result });
                    PumpMessages(TimeSpan.FromMilliseconds(250));
                    Capture(form, Path.Combine(outputDirectory, "ui-complete.png"));

                    var dialogType = assembly.GetType("DiskFill.UI.Dialogs.ConfirmFillDialog", true);
                    var dialog = (Form)Activator.CreateInstance(
                        dialogType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { drive },
                        null);
                    dialog.StartPosition = FormStartPosition.Manual;
                    dialog.Location = new Point(0, 0);
                    try
                    {
                        dialog.Show();
                        PumpMessages(TimeSpan.FromMilliseconds(350));
                        Capture(dialog, Path.Combine(outputDirectory, "ui-confirm.png"));
                    }
                    finally
                    {
                        dialog.Close();
                        dialog.Dispose();
                    }
                }
            }
            finally
            {
                form.Close();
                form.Dispose();
            }
        }

        private static void Capture(Form form, string path)
        {
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(path, ImageFormat.Png);
            }

            Console.WriteLine(path);
        }

        private static void PumpMessages(TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < duration)
            {
                Application.DoEvents();
                Thread.Sleep(25);
            }
        }
    }
}
