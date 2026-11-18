using System;

namespace DiskFill.Core.Services
{
    public static class FileSystemPolicy
    {
        private const long Fat32MaximumSegmentSize = 4L * 1024 * 1024 * 1024 - 1024 * 1024;
        private const long StandardSegmentSize = 16L * 1024 * 1024 * 1024;

        public static long GetSegmentSize(string fileSystem)
        {
            if (string.Equals(fileSystem, "FAT32", StringComparison.OrdinalIgnoreCase))
            {
                return Fat32MaximumSegmentSize;
            }

            return StandardSegmentSize;
        }
    }
}
