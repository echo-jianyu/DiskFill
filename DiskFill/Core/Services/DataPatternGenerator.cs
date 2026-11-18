using System;
using DiskFill.Core.Models;

namespace DiskFill.Core.Services
{
    public sealed class DataPatternGenerator
    {
        private const ulong Seed = 0x9E3779B97F4A7C15UL;

        public void Fill(byte[] buffer, int count, long absoluteOffset, OverwriteMode mode)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count < 0 || count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (mode == OverwriteMode.Zero)
            {
                Array.Clear(buffer, 0, count);
                return;
            }

            var state = Seed ^ unchecked((ulong)absoluteOffset);
            var index = 0;
            while (index <= count - sizeof(ulong))
            {
                state ^= state >> 12;
                state ^= state << 25;
                state ^= state >> 27;
                var value = state * 2685821657736338717UL;
                buffer[index] = (byte)value;
                buffer[index + 1] = (byte)(value >> 8);
                buffer[index + 2] = (byte)(value >> 16);
                buffer[index + 3] = (byte)(value >> 24);
                buffer[index + 4] = (byte)(value >> 32);
                buffer[index + 5] = (byte)(value >> 40);
                buffer[index + 6] = (byte)(value >> 48);
                buffer[index + 7] = (byte)(value >> 56);
                index += sizeof(ulong);
            }

            if (index < count)
            {
                state ^= state >> 12;
                state ^= state << 25;
                state ^= state >> 27;
                var tail = state * 2685821657736338717UL;
                for (var tailIndex = index; tailIndex < count; tailIndex++)
                {
                    buffer[tailIndex] = (byte)(tail >> ((tailIndex - index) * 8));
                }
            }
        }
    }
}
