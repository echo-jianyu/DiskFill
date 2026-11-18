using System;
using System.Collections.Generic;

namespace DiskFill.Core.Infrastructure
{
    public sealed class ThroughputWindow
    {
        private readonly TimeSpan _windowDuration;
        private readonly TimeSpan _minimumSampleDuration;
        private readonly List<Sample> _samples = new List<Sample>();

        public ThroughputWindow(TimeSpan windowDuration, TimeSpan minimumSampleDuration)
        {
            if (windowDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(windowDuration));
            }

            if (minimumSampleDuration <= TimeSpan.Zero || minimumSampleDuration > windowDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSampleDuration));
            }

            _windowDuration = windowDuration;
            _minimumSampleDuration = minimumSampleDuration;
        }

        public void Reset(long totalBytes, TimeSpan timestamp)
        {
            _samples.Clear();
            _samples.Add(new Sample(totalBytes, timestamp));
        }

        public void Record(long totalBytes, TimeSpan timestamp)
        {
            if (_samples.Count == 0
                || totalBytes < _samples[_samples.Count - 1].TotalBytes
                || timestamp < _samples[_samples.Count - 1].Timestamp)
            {
                Reset(totalBytes, timestamp);
                return;
            }

            var last = _samples[_samples.Count - 1];
            if (totalBytes == last.TotalBytes && timestamp == last.Timestamp)
            {
                return;
            }

            _samples.Add(new Sample(totalBytes, timestamp));
            var cutoff = timestamp - _windowDuration;
            var removeCount = 0;
            while (removeCount + 1 < _samples.Count
                && _samples[removeCount + 1].Timestamp <= cutoff)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                _samples.RemoveRange(0, removeCount);
            }
        }

        public bool TryGetBytesPerSecond(out double bytesPerSecond)
        {
            bytesPerSecond = 0;
            if (_samples.Count < 2)
            {
                return false;
            }

            var first = _samples[0];
            var last = _samples[_samples.Count - 1];
            var duration = last.Timestamp - first.Timestamp;
            if (duration < _minimumSampleDuration || duration <= TimeSpan.Zero)
            {
                return false;
            }

            bytesPerSecond = Math.Max(0, last.TotalBytes - first.TotalBytes)
                / duration.TotalSeconds;
            return true;
        }

        private struct Sample
        {
            public Sample(long totalBytes, TimeSpan timestamp)
            {
                TotalBytes = totalBytes;
                Timestamp = timestamp;
            }

            public long TotalBytes { get; private set; }

            public TimeSpan Timestamp { get; private set; }
        }
    }
}
