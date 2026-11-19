using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiskFill.Core.Infrastructure
{
    public sealed class PauseController : IDisposable
    {
        private readonly ManualResetEventSlim _gate = new ManualResetEventSlim(true);
        private bool _disposed;

        public bool IsPaused
        {
            get { return !_gate.IsSet; }
        }

        public void Pause()
        {
            _gate.Reset();
        }

        public void Resume()
        {
            _gate.Set();
        }

        public Task WaitIfPausedAsync(CancellationToken cancellationToken)
        {
            if (_gate.IsSet)
            {
                return Task.FromResult(true);
            }

            return Task.Run(
                () =>
                {
                    _gate.Wait(cancellationToken);
                    return true;
                },
                cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _gate.Dispose();
        }
    }
}
