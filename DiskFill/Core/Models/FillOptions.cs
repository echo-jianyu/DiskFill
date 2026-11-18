namespace DiskFill.Core.Models
{
    public sealed class FillOptions
    {
        public const int WriteBufferSize = 4 * 1024 * 1024;

        public OverwriteMode Mode { get; set; }

        public bool DeleteFilesAfterCompletion { get; set; }
    }
}
