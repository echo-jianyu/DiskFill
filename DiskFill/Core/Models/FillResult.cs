using System;

namespace DiskFill.Core.Models
{
    public enum FillCompletionKind
    {
        Filled = 0,
        Cancelled = 1,
        Failed = 2
    }

    public sealed class FillResult
    {
        public FillCompletionKind CompletionKind { get; set; }

        public long WrittenBytes { get; set; }

        public TimeSpan Elapsed { get; set; }

        public bool ReachedDiskCapacity { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess
        {
            get { return CompletionKind == FillCompletionKind.Filled; }
        }
    }
}
