using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class containing general purpose extension methods.
    /// </summary>
    static class ExtensionMethods
    {
        /// <summary>
        /// Checks if the character is a printable ASCII character.
        /// </summary>
        /// <param name="self">The character to check.</param>
        /// <returns><see langword="true"/> if the character is a printable ASCII character; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsPrintableASCII(this char self)
        {
            return 0x20 <= self && self <= 0x7e;
        }

        /// <summary>
        /// Aligns the value to the next multiple of 4.
        /// </summary>
        /// <remarks>
        /// This will return the same value if the value is already aligned to a multiple of 4.
        /// </remarks>
        /// <param name="self">The value to align.</param>
        /// <returns>The aligned value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Align4(this int self)
        {
            return (self + 3) & ~3;
        }

        /// <summary>
        /// Aligns the value to the next multiple of 4.
        /// </summary>
        /// <remarks>
        /// This will return the next multiple if the value is already aligned to a multiple of 4.
        /// </remarks>
        /// <param name="self">The value to align.</param>
        /// <returns>The aligned value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AlignNext4(this int self)
        {
            return (self + 4) & ~3;
        }

        /// <summary>
        /// Swaps the byte order of the given value to big endian byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ToBigEndian(this int n)
        {
            return (int)ToBigEndian((uint)n);
        }

        /// <summary>
        /// Swaps the byte order of the given value from big endian byte order to the platform byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FromBigEndian(this int n)
        {
            return (int)FromBigEndian((uint)n);
        }

        /// <summary>
        /// Swaps the byte order of the given value to big endian byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ToBigEndian(this uint n)
        {
            return FromBigEndian(n);
        }

        /// <summary>
        /// Swaps the byte order of the given value from big endian byte order to the platform byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint FromBigEndian(this uint n)
        {
            if (BitConverter.IsLittleEndian)
            {
                n = ((n << 8) & 0xFF00FF00U) | ((n >> 8) & 0x00FF00FFU);
                n = (n << 16) | (n >> 16);
            }
            return n;
        }

        /// <summary>
        /// Swaps the byte order of the given value to big endian byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ToBigEndian(this long n)
        {
            return (long)ToBigEndian((ulong)n);
        }

        /// <summary>
        /// Swaps the byte order of the given value from big endian byte order to the platform byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long FromBigEndian(this long n)
        {
            return (long)FromBigEndian((ulong)n);
        }

        /// <summary>
        /// Swaps the byte order of the given value to big endian byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ToBigEndian(this ulong n)
        {
            return FromBigEndian(n);
        }

        /// <summary>
        /// Swaps the byte order of the given value from big endian byte order to the platform byte order.
        /// </summary>
        /// <param name="n">The value to swap the bytes of.</param>
        /// <returns>The byte swapped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong FromBigEndian(this ulong n)
        {
            if (BitConverter.IsLittleEndian)
            {
                n = ((n << 8) & 0xFF00FF00FF00FF00UL) | ((n >> 8) & 0x00FF00FF00FF00FFUL);
                n = ((n << 16) & 0xFFFF0000FFFF0000UL) | ((n >> 16) & 0x0000FFFF0000FFFFUL);
                n = (n << 32) | (n >> 32);
            }
            return n;
        }

        /// <summary>
        /// Frees a GC handle if it has been allocated.
        /// </summary>
        /// <param name="handle">The handle to free.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SafeFree(ref this GCHandle handle)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
                handle = default;
            }
        }
    }
}
