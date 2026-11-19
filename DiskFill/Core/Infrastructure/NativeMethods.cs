using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DiskFill.Core.Infrastructure
{
    internal static class NativeMethods
    {
        internal const uint EsContinuous = 0x80000000;
        internal const uint EsSystemRequired = 0x00000001;

        private const uint IoctlStorageQueryProperty = 0x002D1400;
        private const uint IoctlVolumeGetVolumeDiskExtents = 0x00560000;
        private const uint StorageDeviceSeekPenaltyProperty = 7;
        private const uint PropertyStandardQuery = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetVolumeInformation(
            string rootPathName,
            StringBuilder volumeNameBuffer,
            int volumeNameSize,
            out uint volumeSerialNumber,
            out uint maximumComponentLength,
            out uint fileSystemFlags,
            StringBuilder fileSystemNameBuffer,
            int fileSystemNameSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            FileShare shareMode,
            IntPtr securityAttributes,
            FileMode creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeviceIoControl(
            SafeFileHandle device,
            uint ioControlCode,
            ref StoragePropertyQuery inputBuffer,
            int inputBufferSize,
            IntPtr outputBuffer,
            int outputBufferSize,
            out uint bytesReturned,
            IntPtr overlapped);

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeviceIoControlNoInput(
            SafeFileHandle device,
            uint ioControlCode,
            IntPtr inputBuffer,
            int inputBufferSize,
            IntPtr outputBuffer,
            int outputBufferSize,
            out uint bytesReturned,
            IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint SetThreadExecutionState(uint executionState);

        internal static bool TryGetIncursSeekPenalty(string rootPath, out bool incursSeekPenalty)
        {
            incursSeekPenalty = false;
            if (string.IsNullOrEmpty(rootPath) || rootPath.Length < 2)
            {
                return false;
            }

            var volumePath = @"\\.\" + rootPath.Substring(0, 2);
            using (var handle = CreateFile(
                volumePath,
                0,
                FileShare.ReadWrite,
                IntPtr.Zero,
                FileMode.Open,
                0,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    return false;
                }

                var query = new StoragePropertyQuery
                {
                    PropertyId = StorageDeviceSeekPenaltyProperty,
                    QueryType = PropertyStandardQuery,
                    AdditionalParameters = 0
                };

                var size = Marshal.SizeOf(typeof(DeviceSeekPenaltyDescriptor));
                var output = Marshal.AllocHGlobal(size);
                try
                {
                    if (!DeviceIoControl(
                        handle,
                        IoctlStorageQueryProperty,
                        ref query,
                        Marshal.SizeOf(typeof(StoragePropertyQuery)),
                        output,
                        size,
                        out uint bytesReturned,
                        IntPtr.Zero)
                        || bytesReturned < 9)
                    {
                        return false;
                    }

                    var descriptor = (DeviceSeekPenaltyDescriptor)Marshal.PtrToStructure(
                        output,
                        typeof(DeviceSeekPenaltyDescriptor));
                    incursSeekPenalty = descriptor.IncursSeekPenalty;
                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(output);
                }
            }
        }

        internal static bool TryGetVolumeDiskNumbers(string rootPath, out int[] diskNumbers)
        {
            diskNumbers = null;
            if (string.IsNullOrEmpty(rootPath) || rootPath.Length < 2)
            {
                return false;
            }

            var volumePath = @"\\.\" + rootPath.Substring(0, 2);
            using (var handle = CreateFile(
                volumePath,
                0,
                FileShare.ReadWrite,
                IntPtr.Zero,
                FileMode.Open,
                0,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    return false;
                }

                const int outputSize = 1024;
                var output = Marshal.AllocHGlobal(outputSize);
                try
                {
                    if (!DeviceIoControlNoInput(
                        handle,
                        IoctlVolumeGetVolumeDiskExtents,
                        IntPtr.Zero,
                        0,
                        output,
                        outputSize,
                        out uint bytesReturned,
                        IntPtr.Zero))
                    {
                        return false;
                    }

                    var count = (uint)Marshal.ReadInt32(output);
                    var extentCount = checked((int)count);
                    var extentOffset = (int)Marshal.OffsetOf(
                        typeof(VolumeDiskExtents),
                        nameof(VolumeDiskExtents.Extent));
                    var extentSize = Marshal.SizeOf(typeof(DiskExtent));
                    if (extentCount == 0 || bytesReturned < extentOffset + extentCount * extentSize)
                    {
                        return false;
                    }

                    var numbers = new int[extentCount];
                    for (var index = 0; index < extentCount; index++)
                    {
                        numbers[index] = Marshal.ReadInt32(
                            output,
                            extentOffset + index * extentSize);
                    }

                    diskNumbers = numbers;
                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(output);
                }
            }
        }

        internal static bool TryGetVolumeDetails(
            string rootPath,
            out string label,
            out string fileSystem,
            out string serial)
        {
            var labelBuilder = new StringBuilder(261);
            var fileSystemBuilder = new StringBuilder(261);
            label = string.Empty;
            fileSystem = string.Empty;
            serial = "00000000";

            if (!GetVolumeInformation(
                rootPath,
                labelBuilder,
                labelBuilder.Capacity,
                out uint serialNumber,
                out uint _,
                out uint _,
                fileSystemBuilder,
                fileSystemBuilder.Capacity))
            {
                return false;
            }

            label = labelBuilder.ToString();
            fileSystem = fileSystemBuilder.ToString();
            serial = serialNumber.ToString("X8");
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StoragePropertyQuery
        {
            public uint PropertyId;
            public uint QueryType;
            public byte AdditionalParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DeviceSeekPenaltyDescriptor
        {
            public uint Version;
            public uint Size;
            [MarshalAs(UnmanagedType.U1)]
            public bool IncursSeekPenalty;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct VolumeDiskExtents
        {
            public uint NumberOfDiskExtents;
            public DiskExtent Extent;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DiskExtent
        {
            public uint DiskNumber;
            public long StartingOffset;
            public long ExtentLength;
        }
    }
}
