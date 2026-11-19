using System.Collections.Generic;
using System.Linq;
using DiskFill.Core.Models;

namespace DiskFill.Core.Services
{
    public static class PhysicalDiskSelectionGuard
    {
        public static bool CanAdd(
            IReadOnlyList<DriveTarget> selectedDrives,
            DriveTarget candidate,
            out string reason)
        {
            reason = string.Empty;
            if (candidate == null)
            {
                reason = "未选择磁盘。";
                return false;
            }

            if (selectedDrives == null || selectedDrives.Count == 0)
            {
                return true;
            }

            if (!candidate.HasKnownPhysicalDisk
                || selectedDrives.Any(drive => !drive.HasKnownPhysicalDisk))
            {
                reason = "无法确认卷对应的物理磁盘，不能安全并行写入。";
                return false;
            }

            var conflict = selectedDrives.FirstOrDefault(
                drive => drive.PhysicalDiskNumber == candidate.PhysicalDiskNumber);
            if (conflict != null)
            {
                reason = "目标卷与已选卷位于同一个物理磁盘（"
                    + candidate.PhysicalDiskDescription
                    + "）。同一机械硬盘并行写入会造成磁头争用。";
                return false;
            }

            return true;
        }
    }
}
