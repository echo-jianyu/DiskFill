using System;

namespace DiskFill.Core.Infrastructure
{
    public sealed class ExecutionStateScope : IDisposable
    {
        private const uint EsContinuous = 0x80000000;
        private const uint EsSystemRequired = 0x00000001;
        private bool _active;

        public ExecutionStateScope()
        {
            try
            {
                _active = NativeMethods.SetThreadExecutionState(EsContinuous | EsSystemRequired) != 0;
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not enable the system execution state.", exception);
            }
        }

        public void Dispose()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            try
            {
                NativeMethods.SetThreadExecutionState(EsContinuous);
            }
            catch (Exception exception)
            {
                AppLog.Warn("Could not restore the system execution state.", exception);
            }
        }
    }
}
