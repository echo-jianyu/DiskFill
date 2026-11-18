using System;
using System.IO;

namespace DiskFill.Core.Services
{
    public static class SystemVolumeGuard
    {
        public static string GetProtectionReason(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return "无效的磁盘路径";
            }

            var targetRoot = NormalizeRoot(rootPath);
            if (string.IsNullOrEmpty(targetRoot))
            {
                return "无效的磁盘路径";
            }

            if (RootsEqual(targetRoot, GetPathRoot(Environment.SystemDirectory)))
            {
                return "系统盘受保护";
            }

            if (RootsEqual(targetRoot, GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))))
            {
                return "Windows 所在的磁盘受保护";
            }

            if (RootsEqual(targetRoot, GetPathRoot(AppDomain.CurrentDomain.BaseDirectory)))
            {
                return "程序所在的磁盘受保护";
            }

            try
            {
                var currentDirectoryRoot = GetPathRoot(Directory.GetCurrentDirectory());
                if (RootsEqual(targetRoot, currentDirectoryRoot))
                {
                    return "当前工作目录所在的磁盘受保护";
                }
            }
            catch (Exception)
            {
                // The other guards still protect the important system locations.
            }

            return string.Empty;
        }

        internal static string GetPathRoot(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                return NormalizeRoot(Path.GetPathRoot(fullPath));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        internal static string NormalizeRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return string.Empty;
            }

            var root = rootPath.Trim();
            if (root.Length == 2 && root[1] == ':')
            {
                root += "\\";
            }

            if (!root.EndsWith("\\", StringComparison.Ordinal))
            {
                root += "\\";
            }

            return root.ToUpperInvariant();
        }

        private static bool RootsEqual(string first, string second)
        {
            return !string.IsNullOrEmpty(first)
                && !string.IsNullOrEmpty(second)
                && string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }
    }
}
