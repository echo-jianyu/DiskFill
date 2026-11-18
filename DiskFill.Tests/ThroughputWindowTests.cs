using System;
using DiskFill.Core.Infrastructure;
using Xunit;

namespace DiskFill.Tests
{
    public sealed class ThroughputWindowTests
    {
        private const double Mebibyte = 1024D * 1024D;

        [Fact]
        public void TryGetBytesPerSecond_WaitsForMinimumDuration()
        {
            var window = new ThroughputWindow(
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(2));
            window.Reset(0, TimeSpan.Zero);
            window.Record((long)(50 * Mebibyte), TimeSpan.FromSeconds(1));

            Assert.False(window.TryGetBytesPerSecond(out _));

            window.Record((long)(100 * Mebibyte), TimeSpan.FromSeconds(2));

            Assert.True(window.TryGetBytesPerSecond(out double bytesPerSecond));
            Assert.InRange(bytesPerSecond, 50 * Mebibyte - 1, 50 * Mebibyte + 1);
        }

        [Fact]
        public void TryGetBytesPerSecond_TracksConstantRate()
        {
            var window = new ThroughputWindow(
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(2));
            window.Reset(0, TimeSpan.Zero);

            long totalBytes = 0;
            for (var step = 1; step <= 20; step++)
            {
                totalBytes += (long)(100 * Mebibyte * 0.5);
                window.Record(totalBytes, TimeSpan.FromSeconds(step * 0.5));
            }

            Assert.True(window.TryGetBytesPerSecond(out double bytesPerSecond));
            Assert.InRange(bytesPerSecond, 100 * Mebibyte - 1, 100 * Mebibyte + 1);
        }

        [Fact]
        public void TryGetBytesPerSecond_ExpiresSamplesOutsideTheWindow()
        {
            var window = new ThroughputWindow(
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(2));
            window.Reset(0, TimeSpan.Zero);

            long totalBytes = 0;
            for (var step = 1; step <= 60; step++)
            {
                var seconds = step * 0.5;
                var rate = seconds <= 10 ? 50 : 300;
                totalBytes += (long)(rate * Mebibyte * 0.5);
                window.Record(totalBytes, TimeSpan.FromSeconds(seconds));
            }

            Assert.True(window.TryGetBytesPerSecond(out double bytesPerSecond));
            Assert.InRange(bytesPerSecond, 300 * Mebibyte - 1, 300 * Mebibyte + 1);
        }

        [Fact]
        public void Reset_DiscardsPausedTimeAndOldSamples()
        {
            var window = new ThroughputWindow(
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(2));
            window.Reset(0, TimeSpan.Zero);
            window.Record((long)(50 * Mebibyte), TimeSpan.FromSeconds(1));
            window.Record((long)(100 * Mebibyte), TimeSpan.FromSeconds(2));

            window.Reset((long)(100 * Mebibyte), TimeSpan.FromSeconds(30));
            window.Record((long)(700 * Mebibyte), TimeSpan.FromSeconds(32));

            Assert.True(window.TryGetBytesPerSecond(out double bytesPerSecond));
            Assert.InRange(bytesPerSecond, 300 * Mebibyte - 1, 300 * Mebibyte + 1);
        }
    }
}
