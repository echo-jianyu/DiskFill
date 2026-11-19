using System;

namespace DiskFill.Core.Models
{
    public sealed class DriveTarget
    {
        public string RootPath { get; set; }

        public string DisplayName { get; set; }

        public string Label { get; set; }

        public string FileSystem { get; set; }

        public string VolumeSerial { get; set; }

        public long TotalBytes { get; set; }

        public long AvailableBytes { get; set; }

        public MediaKind MediaKind { get; set; }

        public int? PhysicalDiskNumber { get; set; }

        public bool SpansMultiplePhysicalDisks { get; set; }

        public string IneligibleReason { get; set; }

        public bool IsEligible
        {
            get { return string.IsNullOrEmpty(IneligibleReason); }
        }

        public string MediaDescription
        {
            get
            {
                switch (MediaKind)
                {
                    case MediaKind.Rotational:
                        return "机械硬盘";
                    case MediaKind.SolidState:
                        return "固态硬盘";
                    default:
                        return "介质类型未知";
                }
            }
        }

        public string DriveLetter
        {
            get
            {
                if (string.IsNullOrEmpty(RootPath))
                {
                    return string.Empty;
                }

                return RootPath.Substring(0, Math.Min(2, RootPath.Length));
            }
        }

        public string PhysicalDiskDescription
        {
            get
            {
                if (SpansMultiplePhysicalDisks)
                {
                    return "跨物理磁盘";
                }

                return PhysicalDiskNumber.HasValue
                    ? "物理磁盘 #" + PhysicalDiskNumber.Value
                    : "物理磁盘未知";
            }
        }

        public bool HasKnownPhysicalDisk
        {
            get { return PhysicalDiskNumber.HasValue && !SpansMultiplePhysicalDisks; }
        }

        public override string ToString()
        {
            return RootPath + " " + Label;
        }
    }
}
