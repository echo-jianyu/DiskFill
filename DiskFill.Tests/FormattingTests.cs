using System;
using DiskFill.Core.Infrastructure;
using Xunit;

namespace DiskFill.Tests
{
    public sealed class FormattingTests
    {
        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(1024, "1.00 KB")]
        [InlineData(1536, "1.50 KB")]
        [InlineData(10485760, "10.0 MB")]
        [InlineData(1649267441664, "1.50 TB")]
        public void ByteFormatter_UsesBinaryUnits(long bytes, string expected)
        {
            Assert.Equal(expected, ByteFormatter.Format(bytes));
        }

        [Fact]
        public void ByteFormatter_FormatsRate()
        {
            Assert.Equal("125 MB/s", ByteFormatter.FormatRate(125d * 1024 * 1024));
        }

        [Theory]
        [InlineData(65, "01:05")]
        [InlineData(3665, "01:01:05")]
        public void TimeFormatter_FormatsElapsed(int seconds, string expected)
        {
            Assert.Equal(expected, TimeFormatter.FormatElapsed(TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public void TimeFormatter_FormatsEta()
        {
            Assert.Equal("1 小时 30 分", TimeFormatter.FormatEta(TimeSpan.FromMinutes(90)));
        }
    }
}
