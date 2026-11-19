using System.Collections.Generic;
using DiskFill.Core.Models;
using DiskFill.Core.Services;
using Xunit;

namespace DiskFill.Tests
{
    public sealed class PhysicalDiskSelectionGuardTests
    {
        [Fact]
        public void CanAdd_AllowsVolumesOnDifferentPhysicalDisks()
        {
            var selected = new List<DriveTarget>
            {
                CreateDrive("D:", 1)
            };

            Assert.True(PhysicalDiskSelectionGuard.CanAdd(
                selected,
                CreateDrive("E:", 2),
                out _));
        }

        [Fact]
        public void CanAdd_RejectsVolumesOnTheSamePhysicalDisk()
        {
            var selected = new List<DriveTarget>
            {
                CreateDrive("D:", 1)
            };

            Assert.False(PhysicalDiskSelectionGuard.CanAdd(
                selected,
                CreateDrive("E:", 1),
                out string reason));
            Assert.Contains("同一个物理磁盘", reason);
        }

        [Fact]
        public void CanAdd_RejectsUnknownPhysicalDiskForMultipleSelection()
        {
            var selected = new List<DriveTarget>
            {
                CreateDrive("D:", null)
            };

            Assert.False(PhysicalDiskSelectionGuard.CanAdd(
                selected,
                CreateDrive("E:", 2),
                out string reason));
            Assert.Contains("无法确认", reason);
        }

        private static DriveTarget CreateDrive(string root, int? physicalDisk)
        {
            return new DriveTarget
            {
                RootPath = root + "\\",
                DisplayName = root,
                PhysicalDiskNumber = physicalDisk
            };
        }
    }
}
