using System;
using System.IO;
using System.Text;

namespace DiskFill.Core.Infrastructure
{
    public static class AppLog
    {
        private static readonly object SyncRoot = new object();

        public static string LogDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiskFill",
                    "logs");
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warn(string message, Exception exception = null)
        {
            Write("WARN", message, exception);
        }

        public static void Error(string message, Exception exception = null)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectory);
                    var path = Path.Combine(LogDirectory, "diskfill-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                    var builder = new StringBuilder();
                    builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    builder.Append(" [");
                    builder.Append(level);
                    builder.Append("] ");
                    builder.AppendLine(message ?? string.Empty);

                    if (exception != null)
                    {
                        builder.AppendLine(exception.ToString());
                    }

                    File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never take down a disk operation.
            }
        }
    }
}
