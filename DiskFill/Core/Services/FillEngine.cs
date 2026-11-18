using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiskFill.Core.Infrastructure;
using DiskFill.Core.Models;

namespace DiskFill.Core.Services
{
    public sealed class FillEngine
    {
        private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(220);

        private readonly JobStore _jobStore;
        private readonly DataPatternGenerator _patternGenerator;

        public FillEngine(JobStore jobStore, DataPatternGenerator patternGenerator)
        {
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
            _patternGenerator = patternGenerator ?? throw new ArgumentNullException(nameof(patternGenerator));
        }

        public async Task<FillResult> RunAsync(
            FillJob job,
            IProgress<FillProgress> progress,
            PauseController pauseController,
            CancellationToken cancellationToken)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (pauseController == null)
            {
                throw new ArgumentNullException(nameof(pauseController));
            }

            var stopwatch = Stopwatch.StartNew();
            var totalWritten = _jobStore.GetWrittenBytes(job);
            var buffer = new byte[FillOptions.WriteBufferSize];
            long lastReportBytes = totalWritten;
            long lastReportTicks = stopwatch.ElapsedTicks;
            double smoothedBytesPerSecond = 0;

            job.State = FillJobState.Running;
            _jobStore.SaveQuietly(job);
            Report(progress, job, totalWritten, stopwatch, smoothedBytesPerSecond, pauseController.IsPaused);
            AppLog.Info("Fill job " + job.JobId + " started at byte offset " + totalWritten + ".");

            try
            {
                while (totalWritten < job.PlannedBytes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var wasPaused = pauseController.IsPaused;
                    await pauseController.WaitIfPausedAsync(cancellationToken).ConfigureAwait(false);
                    if (wasPaused)
                    {
                        lastReportBytes = totalWritten;
                        lastReportTicks = stopwatch.ElapsedTicks;
                    }

                    var segment = _jobStore.GetLastSegment(job);
                    if (segment.Path != null && segment.Length >= job.SegmentSize)
                    {
                        segment = new SegmentState(
                            segment.Index + 1,
                            0,
                            _jobStore.GetSegmentPath(job, segment.Index + 1));
                    }

                    var stream = OpenSegment(job, segment.Index);
                    try
                    {
                        var segmentLength = stream.Length;
                        totalWritten = _jobStore.GetWrittenBytes(job);
                        while (segmentLength < job.SegmentSize && totalWritten < job.PlannedBytes)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var wasPausedInSegment = pauseController.IsPaused;
                            await pauseController.WaitIfPausedAsync(cancellationToken).ConfigureAwait(false);
                            if (wasPausedInSegment)
                            {
                                lastReportBytes = totalWritten;
                                lastReportTicks = stopwatch.ElapsedTicks;
                            }

                            var remainingInSegment = job.SegmentSize - segmentLength;
                            var remainingInJob = job.PlannedBytes - totalWritten;
                            var requestedBytes = (int)Math.Min(
                                buffer.Length,
                                Math.Min(remainingInSegment, remainingInJob));

                            _patternGenerator.Fill(buffer, requestedBytes, totalWritten, job.Mode);
                            await stream.WriteAsync(buffer, 0, requestedBytes, cancellationToken).ConfigureAwait(false);

                            totalWritten += requestedBytes;
                            segmentLength += requestedBytes;
                            ReportIfNeeded(
                                progress,
                                job,
                                totalWritten,
                                stopwatch,
                                ref lastReportBytes,
                                ref lastReportTicks,
                                ref smoothedBytesPerSecond,
                                pauseController.IsPaused,
                                false);
                        }

                        stream.Flush(true);
                    }
                    finally
                    {
                        stream.Dispose();
                        totalWritten = _jobStore.GetWrittenBytes(job);
                    }
                }

                job.WrittenBytes = totalWritten;
                job.State = FillJobState.Completed;
                _jobStore.SaveQuietly(job);
                Report(progress, job, totalWritten, stopwatch, smoothedBytesPerSecond, false);
                AppLog.Info("Fill job " + job.JobId + " completed at " + totalWritten + " bytes.");

                return new FillResult
                {
                    CompletionKind = FillCompletionKind.Filled,
                    WrittenBytes = totalWritten,
                    Elapsed = stopwatch.Elapsed,
                    ReachedDiskCapacity = true
                };
            }
            catch (OperationCanceledException)
            {
                totalWritten = _jobStore.GetWrittenBytes(job);
                job.WrittenBytes = totalWritten;
                job.State = FillJobState.Paused;
                _jobStore.SaveQuietly(job);
                Report(progress, job, totalWritten, stopwatch, smoothedBytesPerSecond, true);
                AppLog.Info("Fill job " + job.JobId + " stopped at " + totalWritten + " bytes.");
                return new FillResult
                {
                    CompletionKind = FillCompletionKind.Cancelled,
                    WrittenBytes = totalWritten,
                    Elapsed = stopwatch.Elapsed
                };
            }
            catch (IOException exception) when (IsDiskFull(exception))
            {
                totalWritten = _jobStore.GetWrittenBytes(job);
                job.WrittenBytes = totalWritten;
                job.State = FillJobState.Completed;
                _jobStore.SaveQuietly(job);
                Report(progress, job, totalWritten, stopwatch, smoothedBytesPerSecond, false);
                AppLog.Info("Fill job " + job.JobId + " reached disk capacity at " + totalWritten + " bytes.");
                return new FillResult
                {
                    CompletionKind = FillCompletionKind.Filled,
                    WrittenBytes = totalWritten,
                    Elapsed = stopwatch.Elapsed,
                    ReachedDiskCapacity = true
                };
            }
            catch (Exception exception)
            {
                totalWritten = _jobStore.GetWrittenBytes(job);
                job.WrittenBytes = totalWritten;
                job.State = FillJobState.Failed;
                _jobStore.SaveQuietly(job);
                AppLog.Error("Fill job " + job.JobId + " failed.", exception);
                return new FillResult
                {
                    CompletionKind = FillCompletionKind.Failed,
                    WrittenBytes = totalWritten,
                    Elapsed = stopwatch.Elapsed,
                    ErrorMessage = exception.Message
                };
            }
        }

        internal static bool IsDiskFull(IOException exception)
        {
            var win32Code = exception.HResult & 0xFFFF;
            return win32Code == 39 || win32Code == 112;
        }

        private static FileStream OpenSegment(FillJob job, int index)
        {
            var path = Path.Combine(job.JobDirectory, JobStore.GetSegmentFileName(index));
            var stream = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            stream.Seek(0, SeekOrigin.End);
            return stream;
        }

        private static void ReportIfNeeded(
            IProgress<FillProgress> progress,
            FillJob job,
            long writtenBytes,
            Stopwatch stopwatch,
            ref long lastReportBytes,
            ref long lastReportTicks,
            ref double smoothedBytesPerSecond,
            bool isPaused,
            bool force)
        {
            var elapsedTicks = stopwatch.ElapsedTicks - lastReportTicks;
            if (!force && elapsedTicks < ProgressInterval.TotalSeconds * Stopwatch.Frequency)
            {
                return;
            }

            var elapsedSeconds = (double)elapsedTicks / Stopwatch.Frequency;
            if (elapsedSeconds > 0)
            {
                var currentRate = Math.Max(0, writtenBytes - lastReportBytes) / elapsedSeconds;
                smoothedBytesPerSecond = smoothedBytesPerSecond <= 0
                    ? currentRate
                    : smoothedBytesPerSecond * 0.72 + currentRate * 0.28;
            }

            lastReportBytes = writtenBytes;
            lastReportTicks = stopwatch.ElapsedTicks;
            Report(progress, job, writtenBytes, stopwatch, smoothedBytesPerSecond, isPaused);
        }

        private static void Report(
            IProgress<FillProgress> progress,
            FillJob job,
            long writtenBytes,
            Stopwatch stopwatch,
            double smoothedBytesPerSecond,
            bool isPaused)
        {
            if (progress == null)
            {
                return;
            }

            progress.Report(new FillProgress
            {
                WrittenBytes = writtenBytes,
                TotalBytes = job.PlannedBytes,
                BytesPerSecond = smoothedBytesPerSecond,
                ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                IsPaused = isPaused,
                Message = isPaused ? "任务已暂停" : "顺序写入中"
            });
        }
    }
}
