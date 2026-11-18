namespace DiskFill.Core.Models
{
    public sealed class FillProgress
    {
        public long WrittenBytes { get; set; }

        public long TotalBytes { get; set; }

        public double BytesPerSecond { get; set; }

        public double ElapsedSeconds { get; set; }

        public double Fraction
        {
            get
            {
                if (TotalBytes <= 0)
                {
                    return 0;
                }

                return WrittenBytes >= TotalBytes ? 1 : (double)WrittenBytes / TotalBytes;
            }
        }

        public bool IsPaused { get; set; }

        public string Message { get; set; }
    }
}
