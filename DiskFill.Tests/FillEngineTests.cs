using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;
using DiskFill.Core.Services;
using Xunit;

namespace DiskFill.Tests
{
    public sealed class FillEngineTests
    {
        [Fact]
        public async Task RunAsync_WritesPlannedBytesAcrossSegments()
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                var job = CreateJob(directory, 6L * 1024 * 1024 + 12345, 4L * 1024 * 1024);
                var engine = new FillEngine(new JobStore(), new DataPatternGenerator());

                using (var pause = new PauseController())
                {
                    var result = await engine.RunAsync(
                        job,
                        null,
                        pause,
                        CancellationToken.None);
                    Assert.True(result.IsSuccess);
                }

                var files = Directory.GetFiles(directory, "part-*.bin").OrderBy(path => path).ToArray();
                Assert.Equal(2, files.Length);
                Assert.Equal(job.PlannedBytes, files.Sum(path => new FileInfo(path).Length));
                Assert.Contains(File.ReadAllBytes(files[0]), value => value != 0);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public async Task RunAsync_AppendsToExistingSegment()
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                var job = CreateJob(directory, 3L * 1024 * 1024, 8L * 1024 * 1024);
                File.WriteAllBytes(
                    Path.Combine(directory, JobStore.GetSegmentFileName(1)),
                    new byte[1024 * 1024]);
                var engine = new FillEngine(new JobStore(), new DataPatternGenerator());

                using (var pause = new PauseController())
                {
                    var result = await engine.RunAsync(
                        job,
                        null,
                        pause,
                        CancellationToken.None);
                    Assert.True(result.IsSuccess);
                }

                var segment = Path.Combine(directory, JobStore.GetSegmentFileName(1));
                Assert.Equal(job.PlannedBytes, new FileInfo(segment).Length);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void IsDiskFull_RecognizesWindowsDiskFullCodes()
        {
            var diskFull = new IOException(
                "disk full",
                unchecked((int)0x80070070));
            var other = new IOException(
                "io error",
                unchecked((int)0x8007001F));

            Assert.True(FillEngine.IsDiskFull(diskFull));
            Assert.False(FillEngine.IsDiskFull(other));
        }

        private static FillJob CreateJob(string directory, long plannedBytes, long segmentSize)
        {
            var now = DateTime.UtcNow;
            return new FillJob
            {
                SchemaVersion = FillJob.CurrentSchemaVersion,
                JobId = Guid.NewGuid().ToString("N"),
                VolumeRoot = Path.GetPathRoot(directory),
                VolumeSerial = "TEST0001",
                VolumeSize = 1024L * 1024 * 1024,
                VolumeLabel = "TEST",
                FileSystem = "NTFS",
                Mode = OverwriteMode.FastNonZero,
                PlannedBytes = plannedBytes,
                SegmentSize = segmentSize,
                State = FillJobState.Ready,
                CreatedUtc = now,
                UpdatedUtc = now,
                JobDirectory = directory,
                ManifestPath = Path.Combine(directory, JobStore.ManifestFileName)
            };
        }

        private static string CreateTemporaryDirectory()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "DiskFill.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
