using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;

namespace DiskFill.Core.Services
{
    public sealed class JobStore
    {
        public const string WorkspaceFolderName = ".diskfill";
        public const string ManifestFileName = "session.json";
        public const string SegmentFilePrefix = "part-";
        public const string SegmentFileExtension = ".bin";

        public JobInspection Inspect(DriveTarget drive)
        {
            if (drive == null)
            {
                return new JobInspection { Message = "未选择磁盘" };
            }

            var workspace = GetWorkspacePath(drive.RootPath);
            var manifestPath = GetManifestPath(drive.RootPath);
            if (!Directory.Exists(workspace))
            {
                return new JobInspection();
            }

            if (IsReparsePoint(workspace))
            {
                return new JobInspection
                {
                    Message = "任务目录是重解析点，出于安全原因已拒绝读取。"
                };
            }

            if (!File.Exists(manifestPath))
            {
                if (TryRemoveOrphanWorkspace(workspace))
                {
                    return new JobInspection();
                }

                return new JobInspection
                {
                    Message = "发现残留任务目录，但任务清单缺失。请先手动检查 " + workspace
                };
            }

            try
            {
                var info = new FileInfo(manifestPath);
                if (info.Length > 1024 * 1024)
                {
                    return new JobInspection { Message = "任务清单异常过大，已拒绝读取。" };
                }

                FillJob job;
                using (var stream = File.OpenRead(manifestPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(FillJob));
                    job = serializer.ReadObject(stream) as FillJob;
                }

                if (job == null)
                {
                    return new JobInspection { Message = "无法解析目标盘上的任务清单。" };
                }

                PopulateRuntimePaths(job, drive.RootPath);
                if (!job.IsCompatibleWith(drive))
                {
                    return new JobInspection
                    {
                        Job = job,
                        Message = "任务清单与当前卷不匹配。为保护数据，程序不会自动覆盖该任务。"
                    };
                }

                job.WrittenBytes = GetWrittenBytes(job);
                return new JobInspection { Job = job };
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not read the job manifest at " + manifestPath, exception);
                return new JobInspection { Message = "读取任务清单失败：" + exception.Message };
            }
        }

        public FillJob CreateNew(DriveTarget drive, OverwriteMode mode)
        {
            if (drive == null)
            {
                throw new ArgumentNullException(nameof(drive));
            }

            if (!drive.IsEligible)
            {
                throw new InvalidOperationException(drive.IneligibleReason);
            }

            var inspection = Inspect(drive);
            if (inspection.HasJob || inspection.HasIssue)
            {
                throw new InvalidOperationException(
                    inspection.Message ?? "目标盘上已经存在未完成的 DiskFill 任务。");
            }

            var now = DateTime.UtcNow;
            var job = new FillJob
            {
                SchemaVersion = FillJob.CurrentSchemaVersion,
                JobId = Guid.NewGuid().ToString("N"),
                VolumeRoot = drive.RootPath,
                VolumeSerial = drive.VolumeSerial,
                PhysicalDiskNumber = drive.PhysicalDiskNumber,
                VolumeSize = drive.TotalBytes,
                VolumeLabel = drive.Label,
                FileSystem = drive.FileSystem,
                Mode = mode,
                PlannedBytes = drive.AvailableBytes,
                SegmentSize = FileSystemPolicy.GetSegmentSize(drive.FileSystem),
                State = FillJobState.Ready,
                WrittenBytes = 0,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            PopulateRuntimePaths(job, drive.RootPath);
            Directory.CreateDirectory(job.JobDirectory);
            TryHideDirectory(job.JobDirectory);
            Save(job);

            AppLog.Info(
                "Created job " + job.JobId + " on " + job.VolumeRoot
                + " with planned bytes " + job.PlannedBytes + ".");
            return job;
        }

        public void Save(FillJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (string.IsNullOrEmpty(job.JobDirectory) || string.IsNullOrEmpty(job.ManifestPath))
            {
                throw new InvalidOperationException("The job runtime paths are not initialized.");
            }

            job.UpdatedUtc = DateTime.UtcNow;
            Directory.CreateDirectory(job.JobDirectory);

            var temporaryPath = job.ManifestPath + ".tmp";
            var serializer = new DataContractJsonSerializer(typeof(FillJob));
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough))
            {
                serializer.WriteObject(stream, job);
                stream.Flush(true);
            }

            ReplaceManifest(temporaryPath, job.ManifestPath);
            TryHideFile(job.ManifestPath);
        }

        public void SaveQuietly(FillJob job)
        {
            try
            {
                Save(job);
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not update the job manifest.", exception);
            }
        }

        public long GetWrittenBytes(FillJob job)
        {
            if (job == null || string.IsNullOrEmpty(job.JobDirectory) || !Directory.Exists(job.JobDirectory))
            {
                return 0;
            }

            long total = 0;
            foreach (var file in Directory.EnumerateFiles(
                job.JobDirectory,
                SegmentFilePrefix + "*" + SegmentFileExtension,
                SearchOption.TopDirectoryOnly))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch (Exception exception)
                {
                    AppLog.Warn("Could not inspect segment file " + file, exception);
                }
            }

            return total;
        }

        public SegmentState GetLastSegment(FillJob job)
        {
            if (job == null || !Directory.Exists(job.JobDirectory))
            {
                return new SegmentState(1, 0, null);
            }

            var highest = 0;
            string highestPath = null;
            foreach (var file in Directory.EnumerateFiles(
                job.JobDirectory,
                SegmentFilePrefix + "*" + SegmentFileExtension,
                SearchOption.TopDirectoryOnly))
            {
                var index = ParseSegmentIndex(Path.GetFileName(file));
                if (index > highest)
                {
                    highest = index;
                    highestPath = file;
                }
            }

            if (highest == 0)
            {
                return new SegmentState(1, 0, null);
            }

            var length = new FileInfo(highestPath).Length;
            return new SegmentState(highest, length, highestPath);
        }

        public string GetSegmentPath(FillJob job, int index)
        {
            return Path.Combine(job.JobDirectory, GetSegmentFileName(index));
        }

        public void DeleteJob(FillJob job)
        {
            if (job == null || string.IsNullOrEmpty(job.VolumeRoot))
            {
                throw new ArgumentNullException(nameof(job));
            }

            var expectedWorkspace = NormalizeDirectory(GetWorkspacePath(job.VolumeRoot));
            var actualWorkspace = NormalizeDirectory(job.JobDirectory);
            if (!string.Equals(expectedWorkspace, actualWorkspace, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Refusing to delete a directory outside the DiskFill workspace.");
            }

            if (!Directory.Exists(expectedWorkspace))
            {
                return;
            }

            if (IsReparsePoint(expectedWorkspace))
            {
                throw new InvalidOperationException("Refusing to delete a reparse-point task directory.");
            }

            foreach (var file in Directory.EnumerateFiles(
                expectedWorkspace,
                SegmentFilePrefix + "*" + SegmentFileExtension,
                SearchOption.TopDirectoryOnly))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            var manifest = Path.Combine(expectedWorkspace, ManifestFileName);
            if (File.Exists(manifest))
            {
                File.SetAttributes(manifest, FileAttributes.Normal);
                File.Delete(manifest);
            }

            var temporary = manifest + ".tmp";
            if (File.Exists(temporary))
            {
                File.SetAttributes(temporary, FileAttributes.Normal);
                File.Delete(temporary);
            }

            try
            {
                Directory.Delete(expectedWorkspace, false);
            }
            catch (IOException)
            {
                // Unknown files are intentionally preserved.
            }
            catch (UnauthorizedAccessException)
            {
                // Unknown files are intentionally preserved.
            }

            AppLog.Info("Deleted the DiskFill workspace on " + job.VolumeRoot + ".");
        }

        public static string GetWorkspacePath(string volumeRoot)
        {
            return Path.Combine(volumeRoot, WorkspaceFolderName);
        }

        public static string GetManifestPath(string volumeRoot)
        {
            return Path.Combine(GetWorkspacePath(volumeRoot), ManifestFileName);
        }

        public static string GetSegmentFileName(int index)
        {
            return SegmentFilePrefix + index.ToString("D6") + SegmentFileExtension;
        }

        private static void PopulateRuntimePaths(FillJob job, string volumeRoot)
        {
            job.VolumeRoot = volumeRoot;
            job.JobDirectory = GetWorkspacePath(volumeRoot);
            job.ManifestPath = GetManifestPath(volumeRoot);
        }

        private static int ParseSegmentIndex(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)
                || !fileName.StartsWith(SegmentFilePrefix, StringComparison.OrdinalIgnoreCase)
                || !fileName.EndsWith(SegmentFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var numberLength = fileName.Length
                - SegmentFilePrefix.Length
                - SegmentFileExtension.Length;
            if (numberLength <= 0)
            {
                return 0;
            }

            var numberText = fileName.Substring(SegmentFilePrefix.Length, numberLength);
            return int.TryParse(numberText, out int index) ? index : 0;
        }

        private static void ReplaceManifest(string temporaryPath, string manifestPath)
        {
            if (File.Exists(manifestPath))
            {
                try
                {
                    File.Replace(temporaryPath, manifestPath, null);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (IOException)
                {
                }
            }

            File.Copy(temporaryPath, manifestPath, true);
            File.Delete(temporaryPath);
        }

        private static void TryHideDirectory(string path)
        {
            try
            {
                File.SetAttributes(path, FileAttributes.Directory | FileAttributes.Hidden);
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not hide " + path, exception);
            }
        }

        private static void TryHideFile(string path)
        {
            try
            {
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not hide " + path, exception);
            }
        }

        private static string NormalizeDirectory(string path)
        {
            return Path.GetFullPath(path).TrimEnd('\\');
        }

        private static bool IsReparsePoint(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        private static bool TryRemoveOrphanWorkspace(string workspace)
        {
            try
            {
                if (!Directory.Exists(workspace) || IsReparsePoint(workspace))
                {
                    return false;
                }

                var directories = Directory.GetDirectories(workspace);
                var files = Directory.GetFiles(workspace);
                var temporaryManifest = Path.Combine(workspace, ManifestFileName + ".tmp");
                if (directories.Length > 0
                    || files.Any(file => !string.Equals(
                        file,
                        temporaryManifest,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                foreach (var file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                Directory.Delete(workspace, false);
                return true;
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not remove an orphaned job workspace at " + workspace, exception);
                return false;
            }
        }
    }

    public sealed class SegmentState
    {
        public SegmentState(int index, long length, string path)
        {
            Index = index;
            Length = length;
            Path = path;
        }

        public int Index { get; private set; }

        public long Length { get; private set; }

        public string Path { get; private set; }
    }
}
