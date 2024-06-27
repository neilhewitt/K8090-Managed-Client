using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace K8090.ManagedClient
{
    public static class ByteExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte LowByte(this ushort input)
        {
            return (byte)(input % 256);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte HighByte(this ushort input)
        {
            return (byte)(input / 256); // note this will always be valid, even on big-endian architectures
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool[] GetBits(this byte input, int startIndex, int numberOfBits)
        {
            bool[] output = new bool[numberOfBits];
            for (int i = startIndex; i < startIndex + numberOfBits; i++)
            {
                output[i - startIndex] = input.GetBit(i);
            }
            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SetBits(this byte input, int startIndex, params bool[] bitsToSet)
        {
            byte output = input;
            for (int i = startIndex; i < startIndex + bitsToSet.Length; i++)
            {
                output = output.SetBit(i, bitsToSet[i - startIndex]);
            }
            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SetBit(this byte input, int bitIndex, bool state)
        {
            if (state == true) return (byte)(input | (1 << bitIndex));
            else return (byte)(input & ~(1 << bitIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(this byte input, int bitIndex)
        {
            return (input & (1 << bitIndex)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToWord(this (byte low, byte high) bytePair)
        {
            return (ushort)((bytePair.high * 256) + bytePair.low);
        }
    }

}
