using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a blob argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <param name="copyTo">The array to copy the blob's contents into. Will be created if <see langword="null"/> or
        /// resized if it lacks sufficient capacity.
        /// </param>
        /// <param name="offset">The index in <paramref name="copyTo"/> to start writing at.</param>
        /// <returns>The size of the blob if valid; otherwise, -1.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds
        /// the number of message arguments, or if <paramref name="offset"/> is negative.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public int ReadBlob(int index, ref byte[] copyTo, int offset = 0)
        {
            CheckIndexValid(index);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Cannot be negative.");

            switch (m_Tags[index])
            {
                case TypeTag.Blob:
                {
                    AccessBlobUnchecked(m_Offsets[index], out var srcPtr, out var size);

                    OscUtils.EnsureCapacity(ref copyTo, offset + size);

                    fixed (byte* dstPtr = copyTo)
                    {
                        UnsafeUtility.MemCpy(dstPtr + offset, srcPtr, size);
                    }

                    return size;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the data buffer for a blob argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <param name="blobPtr">A pointer to the start of the blob. Returns <see langword="default"/> if the argument is not a blob.</param>
        /// <param name="length">The length of the blob in bytes.</param>
        /// <returns><see langword="true"/> if the argument is a blob; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public bool TryAccessBlob(int index, out IntPtr blobPtr, out int length)
        {
            CheckIndexValid(index);

            switch (m_Tags[index])
            {
                case TypeTag.Blob:
                {
                    AccessBlobUnchecked(m_Offsets[index], out var ptr, out length);
                    blobPtr = (IntPtr)ptr;
                    return true;
                }
            }

            blobPtr = default;
            length = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AccessBlobUnchecked(int offset, out byte* blobPtr, out int length)
        {
            length = ReadInt32Unchecked(offset);
            blobPtr = ElementPtr + offset + sizeof(int);
        }
    }
}
