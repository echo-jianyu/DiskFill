using System;

namespace DiskFill.Core.Infrastructure
{
    public static class TimeFormatter
    {
        public static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 100)
            {
                return ((int)elapsed.TotalHours).ToString("00") + ":" + elapsed.Minutes.ToString("00");
            }

            if (elapsed.TotalHours >= 1)
            {
                return ((int)elapsed.TotalHours).ToString("00") + ":"
                    + elapsed.Minutes.ToString("00") + ":"
                    + elapsed.Seconds.ToString("00");
            }

            return elapsed.Minutes.ToString("00") + ":" + elapsed.Seconds.ToString("00");
        }

        public static string FormatEta(TimeSpan? remaining)
        {
            if (!remaining.HasValue || remaining.Value.TotalSeconds < 1)
            {
                return "--";
            }

            var value = remaining.Value;
            if (value.TotalDays >= 2)
            {
                return ((int)value.TotalDays) + " 天";
            }

            if (value.TotalHours >= 1)
            {
                var hours = (int)value.TotalHours;
                var minutes = value.Minutes;
                return minutes == 0 ? hours + " 小时" : hours + " 小时 " + minutes + " 分";
            }

            return Math.Max(1, (int)Math.Ceiling(value.TotalMinutes)) + " 分钟";
        }
    }
}
