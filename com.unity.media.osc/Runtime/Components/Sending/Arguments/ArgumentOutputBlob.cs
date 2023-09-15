using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(byte[]))]
    sealed class ArgumentOutputByteArray : ArgumentOutput<byte[]>
    {
        byte[] m_Value;
        int m_Length;

        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Blob };

        /// <inheritdoc />
        protected override ArgumentDirtyFlags UpdateValue(byte[] value)
        {
            var newLength = value?.Length ?? 0;

            // check if the data contents has changed since last updated
            bool changed;

            if (m_Value == null || m_Length != newLength)
            {
                changed = true;
            }
            else
            {
                unsafe
                {
                    fixed (byte* src = m_Value)
                    fixed (byte* dst = value)
                    {
                        changed = UnsafeUtility.MemCmp(dst, src, newLength) != 0;
                    }
                }
            }

            if (!changed)
                return ArgumentDirtyFlags.None;

            // if the data has changed we need to store an updated copy of it
            if (m_Value == null || m_Value.Length < newLength)
            {
                m_Value = new byte[2 * newLength];
            }

            if (newLength > 0)
            {
                unsafe
                {
                    fixed (byte* src = m_Value)
                    fixed (byte* dst = value)
                    {
                        UnsafeUtility.MemCpy(dst, src, newLength);
                    }
                }
            }

            m_Length = newLength;
            return ArgumentDirtyFlags.Value;
        }

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteBlob(m_Value == null ? Span<byte>.Empty : m_Value.AsSpan(0, m_Length));
        }
    }
}
