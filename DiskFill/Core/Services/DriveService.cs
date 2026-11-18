using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;

namespace DiskFill.Core.Services
{
    public sealed class DriveService
    {
        public IReadOnlyList<DriveTarget> GetDrives()
        {
            var results = new List<DriveTarget>();
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not enumerate drives.", exception);
                return results;
            }

            foreach (var drive in drives)
            {
                try
                {
                    if (!drive.IsReady
                        || (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable))
                    {
                        continue;
                    }

                    var target = BuildTarget(drive);
                    if (target != null)
                    {
                        results.Add(target);
                    }
                }
                catch (Exception exception)
                {
                    AppLog.Warn("Could not inspect drive " + drive.Name, exception);
                }
            }

            return results
                .OrderByDescending(drive => drive.IsEligible)
                .ThenByDescending(drive => drive.MediaKind == MediaKind.Rotational)
                .ThenByDescending(drive => drive.AvailableBytes)
                .ToList();
        }

        public DriveTarget Refresh(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return null;
            }

            var drive = new DriveInfo(rootPath);
            if (!drive.IsReady
                || (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable))
            {
                return null;
            }

            return BuildTarget(drive);
        }

        private static DriveTarget BuildTarget(DriveInfo drive)
        {
            var label = string.Empty;
            var fileSystem = string.Empty;
            var serial = "00000000";
            NativeMethods.TryGetVolumeDetails(drive.RootDirectory.FullName, out label, out fileSystem, out serial);

            if (string.IsNullOrWhiteSpace(label))
            {
                label = "本地磁盘";
            }

            if (string.IsNullOrWhiteSpace(fileSystem))
            {
                fileSystem = drive.DriveFormat;
            }

            var mediaKind = MediaKind.Unknown;
            if (NativeMethods.TryGetIncursSeekPenalty(drive.RootDirectory.FullName, out bool incursSeekPenalty))
            {
                mediaKind = incursSeekPenalty ? MediaKind.Rotational : MediaKind.SolidState;
            }

            var protectionReason = SystemVolumeGuard.GetProtectionReason(drive.RootDirectory.FullName);
            var ineligibleReason = protectionReason;
            if (string.IsNullOrEmpty(ineligibleReason) && mediaKind == MediaKind.SolidState)
            {
                ineligibleReason = "固态硬盘请使用厂商 Secure Erase 工具";
            }

            if (drive.AvailableFreeSpace < 64L * 1024 * 1024)
            {
                ineligibleReason = string.IsNullOrEmpty(ineligibleReason)
                    ? "可用空间不足 64 MB"
                    : ineligibleReason;
            }

            return new DriveTarget
            {
                RootPath = drive.RootDirectory.FullName,
                DisplayName = drive.Name.TrimEnd('\\'),
                Label = label,
                FileSystem = fileSystem,
                VolumeSerial = serial,
                TotalBytes = drive.TotalSize,
                AvailableBytes = drive.AvailableFreeSpace,
                MediaKind = mediaKind,
                IneligibleReason = ineligibleReason
            };
        }
    }
}
