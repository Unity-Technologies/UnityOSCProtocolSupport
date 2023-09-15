using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A struct used to write OSC Messages.
    /// </summary>
    public unsafe struct OscWriter
    {
        /// <summary>
        /// The buffer to write to.
        /// </summary>
        byte* BufferPtr { get; }

        /// <summary>
        /// The size of the buffer in bytes.
        /// </summary>
        int BufferLength { get; }

        /// <summary>
        /// The number of bytes currently written to the buffer.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OscWriter"/> instance.
        /// </summary>
        /// <param name="bufferPtr">The buffer to write to.</param>
        /// <param name="bufferLength">The size of the buffer in bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferLength"/> is negative.</exception>
        public OscWriter(byte* bufferPtr, int bufferLength)
        {
            if (bufferLength < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferLength), bufferLength, "Cannot be negative.");

            BufferPtr = bufferPtr;
            BufferLength = bufferLength;
            Offset = 0;
        }

        /// <summary>
        /// Gets an <see cref="OscWriter"/> that begins relative to the last byte written by this writer.
        /// </summary>
        /// <returns>A new <see cref="OscWriter"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OscWriter FromCurrent()
        {
            return new OscWriter(BufferPtr + Offset, BufferLength - Offset);
        }

        /// <summary>
        /// Resets the offset to the start of the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Offset = 0;
        }

        /// <summary>
        /// Sets the offset relative to the start of the buffer.
        /// </summary>
        /// <param name="offset">The new offset.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> is negative or exceeds the buffer length.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOffset(int offset)
        {
            if (offset < 0 || offset >= BufferLength)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Cannot exceed buffer bounds.");

            Offset = offset;
        }

        /// <summary>
        /// Write <see cref="OscConstants.BundlePrefix"/> into the message buffer.
        /// </summary>
        /// <remarks>
        /// This is used to indicate the start of an OSC bundle.
        /// </remarks>
        public void WriteBundlePrefix()
        {
            const int size = 8;

            EnsureCapacity(size);

            var ptr = (long*)(BufferPtr + Offset);
            *ptr = OscConstants.BundlePrefixLong;
            Offset += size;
        }

        /// <summary>
        /// Write a 32-bit integer element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteInt32(int data)
        {
            EnsureCapacity(sizeof(int));

            var ptr = (int*)(BufferPtr + Offset);
            *ptr = data.ToBigEndian();
            Offset += sizeof(int);
        }

        /// <summary>
        /// Write a 64-bit integer element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteInt64(long data)
        {
            EnsureCapacity(sizeof(long));

            var ptr = (long*)(BufferPtr + Offset);
            *ptr = data.ToBigEndian();
            Offset += sizeof(long);
        }

        /// <summary>
        /// Write a 32-bit floating point element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat32(float data)
        {
            WriteInt32(*(int*)&data);
        }

        /// <summary>
        /// Write a 64-bit floating point element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat64(double data)
        {
            WriteInt64(*(long*)&data);
        }

        /// <summary>
        /// Write a single ASCII character element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteChar(char data)
        {
            EnsureCapacity(4);

            // char is written in the last byte of the 4-byte block
            BufferPtr[Offset++] = 0;
            BufferPtr[Offset++] = 0;
            BufferPtr[Offset++] = 0;
            BufferPtr[Offset++] = (byte)data;
        }

        /// <summary>
        /// Write an ASCII string element into the message buffer.
        /// </summary>
        /// <remarks>
        /// The string must only use ASCII characters. It is up to the caller to check for correctness.
        /// </remarks>
        /// <param name="data">The element to write.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/> is null.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        public void WriteString(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            EnsureCapacity(data.Length + 4);

            foreach (var chr in data)
            {
                BufferPtr[Offset++] = (byte)chr;
            }

            TerminateString();
        }

        /// <summary>
        /// Write an ASCII string element into the message buffer.
        /// </summary>
        /// <remarks>
        /// The string must only use ASCII characters. It is up to the caller to check for correctness.
        /// </remarks>
        /// <param name="data">The element to write.</param>
        public void WriteString(NativeSlice<byte> data)
        {
            EnsureCapacity(data.Length + 4);

            UnsafeUtility.MemCpy(BufferPtr + Offset, data.GetUnsafeReadOnlyPtr(), data.Length);
            Offset += data.Length;

            TerminateString();
        }

        /// <summary>
        /// Write an ASCII string element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteString(in OscAddress data)
        {
            EnsureCapacity(data.Length + 4);

            UnsafeUtility.MemCpy(BufferPtr + Offset, data.Pointer, data.Length);
            Offset += data.Length;

            TerminateString();
        }

        /// <summary>
        /// Write a blob element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteBlob(ReadOnlySpan<byte> data)
        {
            EnsureCapacity(sizeof(int) + data.Length + 3);

            // prefix the blob size to the message
            WriteInt32(data.Length);

            // copy the blob data
            fixed (byte* dataPtr = data)
            {
                UnsafeUtility.MemCpy(BufferPtr + Offset, dataPtr, data.Length);
            }
            Offset += data.Length;

            TerminateBlob();
        }

        /// <summary>
        /// Write a blob element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteBlob(NativeSlice<byte> data)
        {
            EnsureCapacity(sizeof(int) + data.Length + 3);

            // prefix the blob size to the message
            WriteInt32(data.Length);

            // copy the blob data
            UnsafeUtility.MemCpy(BufferPtr + Offset, data.GetUnsafeReadOnlyPtr(), data.Length);
            Offset += data.Length;

            TerminateBlob();
        }

        /// <summary>
        /// Write a 32-bit RGBA color element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteColor(Color32 data)
        {
            EnsureCapacity(sizeof(Color32));

            var ptr = (Color32*)(BufferPtr + Offset);
            *ptr = data;
            Offset += sizeof(Color32);
        }

        /// <summary>
        /// Write a MIDI message element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteMidi(MidiMessage data)
        {
            EnsureCapacity(sizeof(MidiMessage));

            var ptr = (MidiMessage*)(BufferPtr + Offset);
            *ptr = data;
            Offset += sizeof(MidiMessage);
        }

        /// <summary>
        /// Write a time tag element into the message buffer.
        /// </summary>
        /// <param name="data">The element to write.</param>
        public void WriteTimeTag(TimeTag data)
        {
            EnsureCapacity(sizeof(TimeTag));

            var ptr = (uint*)(BufferPtr + Offset);
            data.ToBigEndianBytes(ptr);
            Offset += sizeof(TimeTag);
        }

        /// <summary>
        /// Write custom data into the message buffer.
        /// </summary>
        /// <remarks>
        /// It is up to the caller to ensure the buffer is valid.
        /// </remarks>
        /// <param name="dataPtr">The data to write.</param>
        /// <param name="length">The number of bytes to write.</param>
        internal void WriteCustom(void* dataPtr, int length)
        {
            EnsureCapacity(length);

            UnsafeUtility.MemCpy(BufferPtr + Offset, dataPtr, length);
            Offset += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureCapacity(int numBytesToWrite)
        {
            if (BufferLength < Offset + numBytesToWrite)
            {
                throw new InvalidOperationException($"Write operation would overflow the buffer.");
            }
        }

        void TerminateString()
        {
            // the string must be followed by 1-4 null terminators to pad the total string length to a 4-byte alignment.
            var padding = 4 - (Offset % 4);

            for (var i = 0; i < padding; i++)
            {
                BufferPtr[Offset++] = 0;
            }
        }

        void TerminateBlob()
        {
            // write any trailing zeros necessary to pad the total size to a 4-byte alignment
            var padding = Offset.Align4() - Offset;

            for (var i = 0; i < padding; i++)
            {
                BufferPtr[Offset++] = 0;
            }
        }
    }
}
