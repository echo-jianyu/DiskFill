using System;
using System.IO;
using DiskFill.Core.Models;
using DiskFill.Core.Services;
using Xunit;

namespace DiskFill.Tests
{
    public sealed class StoragePolicyTests
    {
        [Fact]
        public void Fat32_UsesSegmentBelowFourGigabytes()
        {
            var size = FileSystemPolicy.GetSegmentSize("FAT32");

            Assert.True(size < uint.MaxValue);
            Assert.True(size > 4L * 1024 * 1024 * 1024 - 2L * 1024 * 1024);
        }

        [Fact]
        public void Ntfs_UsesStandardSixteenGigabyteSegment()
        {
            Assert.Equal(16L * 1024 * 1024 * 1024, FileSystemPolicy.GetSegmentSize("NTFS"));
        }

        [Fact]
        public void SegmentFileNames_AreStableAndSortable()
        {
            Assert.Equal("part-000012.bin", JobStore.GetSegmentFileName(12));
        }

        [Fact]
        public void SystemVolumeGuard_AlwaysProtectsTheSystemDrive()
        {
            var reason = SystemVolumeGuard.GetProtectionReason(
                Path.GetPathRoot(Environment.SystemDirectory));

            Assert.False(string.IsNullOrEmpty(reason));
        }

        [Fact]
        public void SystemVolumeGuard_NormalizesDriveRoots()
        {
            Assert.Equal("D:\\", SystemVolumeGuard.NormalizeRoot("d:"));
        }

        [Fact]
        public void FillJob_CompatibilityRequiresSerialAndSize()
        {
            var drive = new DriveTarget
            {
                RootPath = "D:\\",
                VolumeSerial = "A1B2C3D4",
                TotalBytes = 1000
            };
            var job = new FillJob
            {
                SchemaVersion = FillJob.CurrentSchemaVersion,
                VolumeSerial = "A1B2C3D4",
                VolumeSize = 1000
            };

            Assert.True(job.IsCompatibleWith(drive));

            drive.VolumeSerial = "FFFFFFFF";
            Assert.False(job.IsCompatibleWith(drive));
        }

        [Fact]
        public void JobStore_RemovesAnOrphanedTemporaryManifest()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "DiskFill.Tests",
                Guid.NewGuid().ToString("N"));
            var workspace = JobStore.GetWorkspacePath(root);
            Directory.CreateDirectory(workspace);
            File.WriteAllText(
                Path.Combine(workspace, JobStore.ManifestFileName + ".tmp"),
                "incomplete");

            try
            {
                var inspection = new JobStore().Inspect(new DriveTarget
                {
                    RootPath = root,
                    VolumeSerial = "TEST0001",
                    TotalBytes = 1024
                });

                Assert.False(inspection.HasIssue);
                Assert.False(inspection.HasJob);
                Assert.False(Directory.Exists(workspace));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
