using System.Linq;
using DiskFill.Core.Models;
using DiskFill.Core.Services;
using Xunit;

namespace DiskFill.Tests
{
    public sealed class DataPatternGeneratorTests
    {
        [Fact]
        public void FastMode_IsDeterministicAndNonZero()
        {
            var generator = new DataPatternGenerator();
            var first = new byte[16384];
            var second = new byte[16384];

            generator.Fill(first, first.Length, 1234567, OverwriteMode.FastNonZero);
            generator.Fill(second, second.Length, 1234567, OverwriteMode.FastNonZero);

            Assert.Equal(first, second);
            Assert.Contains(first, value => value != 0);
        }

        [Fact]
        public void FastMode_ChangesWithOffset()
        {
            var generator = new DataPatternGenerator();
            var first = new byte[4096];
            var second = new byte[4096];

            generator.Fill(first, first.Length, 0, OverwriteMode.FastNonZero);
            generator.Fill(second, second.Length, 4096, OverwriteMode.FastNonZero);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void ZeroMode_ClearsEveryByte()
        {
            var generator = new DataPatternGenerator();
            var buffer = Enumerable.Repeat((byte)0xCC, 4099).ToArray();

            generator.Fill(buffer, buffer.Length, 99, OverwriteMode.Zero);

            Assert.All(buffer, value => Assert.Equal(0, value));
        }
    }
}
