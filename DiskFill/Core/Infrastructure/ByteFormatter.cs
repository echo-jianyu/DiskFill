using System.Globalization;

namespace DiskFill.Core.Infrastructure
{
    public static class ByteFormatter
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

        public static string Format(long bytes)
        {
            return FormatBytes(bytes);
        }

        public static string FormatRate(double bytesPerSecond)
        {
            if (bytesPerSecond <= 0 || double.IsNaN(bytesPerSecond) || double.IsInfinity(bytesPerSecond))
            {
                return "--";
            }

            return FormatBytes(bytesPerSecond) + "/s";
        }

        private static string FormatBytes(double bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            var unitIndex = 0;
            while (bytes >= 1024 && unitIndex < Units.Length - 1)
            {
                bytes /= 1024;
                unitIndex++;
            }

            string format;
            if (unitIndex == 0)
            {
                format = "0";
            }
            else if (bytes < 10)
            {
                format = "0.00";
            }
            else if (bytes < 100)
            {
                format = "0.0";
            }
            else
            {
                format = "0";
            }

            return bytes.ToString(format, CultureInfo.InvariantCulture) + " " + Units[unitIndex];
        }
    }
}
