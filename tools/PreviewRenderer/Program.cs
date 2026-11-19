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
                    var sessionType = mainFormType.GetNestedType("RunSession", BindingFlags.NonPublic);
                    var runSessions = (IList)mainFormType.GetField(
                        "_runSessions",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(form);
                    runSessions.Clear();
                    var session = Activator.CreateInstance(
                        sessionType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { fillJob },
                        null);
                    runSessions.Add(session);
                    mainFormType.GetField(
                        "_runStopwatch",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(form, Stopwatch.StartNew());
                    mainFormType.GetMethod(
                        "ApplyRunningState",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { runSessions });

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
                        .Invoke(form, new[] { session, progress });
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

                    var driveTargetType = assembly.GetType("DiskFill.Core.Models.DriveTarget", true);
                    var previewDriveOne = CreatePreviewDrive(
                        driveTargetType,
                        drive,
                        "D:\\",
                        "D:",
                        "测试磁盘 A",
                        "PREVIEW0001",
                        2,
                        340L * 1024 * 1024 * 1024);
                    var previewDriveTwo = CreatePreviewDrive(
                        driveTargetType,
                        drive,
                        "E:\\",
                        "E:",
                        "测试磁盘 B",
                        "PREVIEW0002",
                        3,
                        260L * 1024 * 1024 * 1024);
                    var driveCardType = assembly.GetType("DiskFill.UI.Controls.DriveCard", true);
                    var selectedCards = (IList)mainFormType.GetField(
                        "_selectedCards",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(form);
                    selectedCards.Clear();
                    selectedCards.Add(Activator.CreateInstance(
                        driveCardType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { previewDriveOne },
                        null));
                    selectedCards.Add(Activator.CreateInstance(
                        driveCardType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { previewDriveTwo },
                        null));
                    mainFormType.GetField("_selectedDrive", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(form, previewDriveOne);
                    mainFormType.GetField("_selectedInspection", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(form, null);
                    mainFormType.GetMethod(
                        "UpdateMultiSelectionPanel",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, null);

                    runSessions.Clear();
                    var multiJobOne = CreatePreviewJob(
                        fillJobType,
                        assembly,
                        "D:\\",
                        "PREVIEW0001",
                        340L * 1024 * 1024 * 1024);
                    var multiJobTwo = CreatePreviewJob(
                        fillJobType,
                        assembly,
                        "E:\\",
                        "PREVIEW0002",
                        260L * 1024 * 1024 * 1024);
                    var multiSessionOne = Activator.CreateInstance(
                        sessionType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { multiJobOne },
                        null);
                    var multiSessionTwo = Activator.CreateInstance(
                        sessionType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { multiJobTwo },
                        null);
                    runSessions.Add(multiSessionOne);
                    runSessions.Add(multiSessionTwo);
                    mainFormType.GetField(
                        "_runStopwatch",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(form, Stopwatch.StartNew());
                    var screenStateType = mainFormType.GetNestedType("ScreenState", BindingFlags.NonPublic);
                    mainFormType.GetField("_screenState", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(form, Enum.ToObject(screenStateType, 1));
                    mainFormType.GetMethod(
                        "InitializeRunProgressCards",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, null);
                    mainFormType.GetMethod(
                        "ApplyRunningState",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { runSessions });

                    var multiProgressOne = CreatePreviewProgress(
                        progressType,
                        240L * 1024 * 1024 * 1024,
                        340L * 1024 * 1024 * 1024,
                        132D * 1024D * 1024D,
                        7200D);
                    var multiProgressTwo = CreatePreviewProgress(
                        progressType,
                        128L * 1024 * 1024 * 1024,
                        260L * 1024 * 1024 * 1024,
                        96D * 1024D * 1024D,
                        7200D);
                    mainFormType.GetMethod(
                        "OnFillProgress",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { multiSessionOne, multiProgressOne });
                    mainFormType.GetMethod(
                        "OnFillProgress",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { multiSessionTwo, multiProgressTwo });
                    PumpMessages(TimeSpan.FromMilliseconds(250));
                    Capture(form, Path.Combine(outputDirectory, "ui-running-multi.png"));

                    var multiResult = Activator.CreateInstance(resultType);
                    resultType.GetProperty("WrittenBytes").SetValue(
                        multiResult,
                        600L * 1024 * 1024 * 1024,
                        null);
                    resultType.GetProperty("Elapsed").SetValue(multiResult, TimeSpan.FromHours(2.1D), null);
                    resultType.GetProperty("CompletionKind").SetValue(
                        multiResult,
                        Enum.ToObject(
                            assembly.GetType("DiskFill.Core.Models.FillCompletionKind", true),
                            0),
                        null);
                    mainFormType.GetMethod(
                        "ShowCompletedState",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new[] { multiResult });
                    PumpMessages(TimeSpan.FromMilliseconds(250));
                    Capture(form, Path.Combine(outputDirectory, "ui-complete-multi.png"));

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

                    var multiDriveArray = Array.CreateInstance(driveTargetType, 2);
                    multiDriveArray.SetValue(previewDriveOne, 0);
                    multiDriveArray.SetValue(previewDriveTwo, 1);
                    var multiDialog = (Form)Activator.CreateInstance(
                        dialogType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new object[] { multiDriveArray },
                        null);
                    multiDialog.StartPosition = FormStartPosition.Manual;
                    multiDialog.Location = new Point(0, 0);
                    try
                    {
                        multiDialog.Show();
                        PumpMessages(TimeSpan.FromMilliseconds(350));
                        Capture(multiDialog, Path.Combine(outputDirectory, "ui-confirm-multi.png"));
                    }
                    finally
                    {
                        multiDialog.Close();
                        multiDialog.Dispose();
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

        private static object CreatePreviewDrive(
            Type driveTargetType,
            object source,
            string root,
            string displayName,
            string label,
            string serial,
            int physicalDisk,
            long availableBytes)
        {
            var drive = Activator.CreateInstance(driveTargetType);
            driveTargetType.GetProperty("RootPath").SetValue(drive, root, null);
            driveTargetType.GetProperty("DisplayName").SetValue(drive, displayName, null);
            driveTargetType.GetProperty("Label").SetValue(drive, label, null);
            driveTargetType.GetProperty("FileSystem").SetValue(drive, "NTFS", null);
            driveTargetType.GetProperty("VolumeSerial").SetValue(drive, serial, null);
            driveTargetType.GetProperty("TotalBytes").SetValue(
                drive,
                availableBytes + 20L * 1024 * 1024 * 1024,
                null);
            driveTargetType.GetProperty("AvailableBytes").SetValue(drive, availableBytes, null);
            driveTargetType.GetProperty("MediaKind").SetValue(
                drive,
                source.GetType().GetProperty("MediaKind").GetValue(source, null),
                null);
            driveTargetType.GetProperty("PhysicalDiskNumber").SetValue(
                drive,
                physicalDisk,
                null);
            driveTargetType.GetProperty("SpansMultiplePhysicalDisks").SetValue(drive, false, null);
            driveTargetType.GetProperty("IneligibleReason").SetValue(drive, string.Empty, null);
            return drive;
        }

        private static object CreatePreviewJob(
            Type fillJobType,
            Assembly assembly,
            string root,
            string serial,
            long plannedBytes)
        {
            var job = Activator.CreateInstance(fillJobType);
            fillJobType.GetProperty("JobId").SetValue(job, Guid.NewGuid().ToString("N"), null);
            fillJobType.GetProperty("VolumeRoot").SetValue(job, root, null);
            fillJobType.GetProperty("VolumeSerial").SetValue(job, serial, null);
            fillJobType.GetProperty("Mode").SetValue(
                job,
                Enum.ToObject(assembly.GetType("DiskFill.Core.Models.OverwriteMode", true), 0),
                null);
            fillJobType.GetProperty("PlannedBytes").SetValue(job, plannedBytes, null);
            fillJobType.GetProperty("WrittenBytes").SetValue(job, 0L, null);
            return job;
        }

        private static object CreatePreviewProgress(
            Type progressType,
            long writtenBytes,
            long totalBytes,
            double bytesPerSecond,
            double elapsedSeconds)
        {
            var progress = Activator.CreateInstance(progressType);
            progressType.GetProperty("WrittenBytes").SetValue(progress, writtenBytes, null);
            progressType.GetProperty("TotalBytes").SetValue(progress, totalBytes, null);
            progressType.GetProperty("BytesPerSecond").SetValue(progress, bytesPerSecond, null);
            progressType.GetProperty("ElapsedSeconds").SetValue(progress, elapsedSeconds, null);
            return progress;
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
