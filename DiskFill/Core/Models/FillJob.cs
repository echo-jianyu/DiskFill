using System;
using System.Runtime.Serialization;

namespace DiskFill.Core.Models
{
    [DataContract]
    public sealed class FillJob
    {
        public const int CurrentSchemaVersion = 1;

        [DataMember(Order = 1)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 2)]
        public string JobId { get; set; }

        [DataMember(Order = 3)]
        public string VolumeRoot { get; set; }

        [DataMember(Order = 4)]
        public string VolumeSerial { get; set; }

        [DataMember(Order = 5)]
        public long VolumeSize { get; set; }

        [DataMember(Order = 6)]
        public string VolumeLabel { get; set; }

        [DataMember(Order = 7)]
        public string FileSystem { get; set; }

        [DataMember(Order = 8)]
        public OverwriteMode Mode { get; set; }

        [DataMember(Order = 9)]
        public long PlannedBytes { get; set; }

        [DataMember(Order = 10)]
        public long SegmentSize { get; set; }

        [DataMember(Order = 11)]
        public FillJobState State { get; set; }

        [DataMember(Order = 12)]
        public long WrittenBytes { get; set; }

        [DataMember(Order = 13)]
        public DateTime CreatedUtc { get; set; }

        [DataMember(Order = 14)]
        public DateTime UpdatedUtc { get; set; }

        [DataMember(Order = 15, EmitDefaultValue = false)]
        public int? PhysicalDiskNumber { get; set; }

        [IgnoreDataMember]
        public string JobDirectory { get; set; }

        [IgnoreDataMember]
        public string ManifestPath { get; set; }

        public bool IsCompatibleWith(DriveTarget drive)
        {
            if (drive == null)
            {
                return false;
            }

            var physicalDiskMatches = !PhysicalDiskNumber.HasValue
                || !drive.PhysicalDiskNumber.HasValue
                || PhysicalDiskNumber.Value == drive.PhysicalDiskNumber.Value;
            return string.Equals(VolumeSerial, drive.VolumeSerial, StringComparison.OrdinalIgnoreCase)
                && VolumeSize == drive.TotalBytes
                && physicalDiskMatches
                && SchemaVersion == CurrentSchemaVersion;
        }
    }
}
