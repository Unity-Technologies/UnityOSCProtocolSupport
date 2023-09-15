using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to read OSC Messages.
    /// </summary>
    public sealed unsafe partial class OscMessage : OscBundleElement
    {
        TypeTag[] m_Tags;
        int[] m_Offsets;
        int m_AddressLength;

        /// <summary>
        /// The number of arguments in the OSC Message.
        /// </summary>
        public int ArgumentCount { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OscMessage"/> instance.
        /// </summary>
        /// <param name="bufferPtr">The buffer containing the bundle element.</param>
        /// <param name="capacity">The initial argument capacity of the message.</param>
        internal OscMessage(byte* bufferPtr, int capacity = 8) : base(bufferPtr)
        {
            m_Tags = new TypeTag[capacity];
            m_Offsets = new int[capacity];
        }

        /// <summary>
        /// Gets the address pattern of this message.
        /// </summary>
        /// <remarks>
        /// This is used to determine which callbacks on the server will receive this message.
        /// </remarks>
        /// <returns>A new address pattern.</returns>
        public OscAddress GetAddressPattern()
        {
            return new OscAddress(ElementPtr, m_AddressLength);
        }

        /// <summary>
        /// Gets the OSC Type Tag for an argument.
        /// </summary>
        /// <param name="index">The index of the argument to get the tag for.</param>
        /// <returns>The type of the argument.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public TypeTag GetTag(int index)
        {
            CheckIndexValid(index);

            return m_Tags[index];
        }

        /// <inheritdoc/>
        protected override bool OnParse()
        {
            // Address length here doesn't include the null terminator and alignment padding.
            if (!TryGetStringLength(ElementPtr, out m_AddressLength) || m_AddressLength == 0)
            {
                return false;
            }

            // parse the tag string
            var tagsOffset = m_AddressLength.AlignNext4();

            if (!TryParseTags(tagsOffset, out var tagsLength))
            {
                return false;
            }

            // determine where the argument data is for each tag
            var argumentsOffset = tagsOffset + tagsLength;

            return TryFindOffsets(argumentsOffset);
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        bool TryParseTags(int tagsOffset, out int tagStringLength)
        {
            ArgumentCount = 0;

            // the tag string should not start after the end of the message buffer
            if (ElementSize <= tagsOffset)
            {
                tagStringLength = default;
                return false;
            }

            // according to the OSC spec older implementations may not include a tag string
            if (ElementPtr[tagsOffset] != ',')
            {
                tagStringLength = 0;
                return true;
            }

            // skip the starting comma
            var offset = tagsOffset + 1;

            // scan ahead to find how many tags are used
            if (!TryGetStringLength(ElementPtr + offset, out var tagCount))
            {
                tagStringLength = default;
                return false;
            }

            OscUtils.EnsureCapacity(ref m_Tags, tagCount);

            // parse the tag string
            while (true)
            {
                var tag = (TypeTag)ElementPtr[offset];

                switch (tag)
                {
                    case TypeTag.False:
                    case TypeTag.Infinitum:
                    case TypeTag.Nil:
                    case TypeTag.AltTypeString:
                    case TypeTag.True:
                    case TypeTag.Blob:
                    case TypeTag.AsciiChar32:
                    case TypeTag.Float64:
                    case TypeTag.Float32:
                    case TypeTag.Int64:
                    case TypeTag.Int32:
                    case TypeTag.MIDI:
                    case TypeTag.Color32:
                    case TypeTag.String:
                    case TypeTag.TimeTag:
                    {
                        m_Tags[ArgumentCount++] = tag;
                        break;
                    }
                    case TypeTag.ArrayStart:
                    case TypeTag.ArrayEnd:
                    {
                        // Ignore arrays for now, they are not very important to support.
                        // Nested arrays (if the standard permits them, not specified) would be a little
                        // tricky to handle.
                        break;
                    }
                    case 0:
                    {
                        // a null terminator was encountered marking the end of the tag string
                        tagStringLength = (offset - tagsOffset).AlignNext4();
                        return true;
                    }
                    default:
                    {
                        // unsupported argument detected, we don't know how to parse the message
                        tagStringLength = default;
                        return false;
                    }
                }

                offset++;
            }
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        bool TryFindOffsets(int offset)
        {
            OscUtils.EnsureCapacity(ref m_Offsets, ArgumentCount);

            for (var i = 0; i < ArgumentCount; i++)
            {
                if (offset > ElementSize)
                {
                    return false;
                }

                m_Offsets[i] = offset;

                switch (m_Tags[i])
                {
                    case TypeTag.Int32:
                    case TypeTag.Float32:
                    case TypeTag.Color32:
                    case TypeTag.AsciiChar32:
                    case TypeTag.MIDI:
                    {
                        offset += 4;
                        break;
                    }
                    case TypeTag.Float64:
                    case TypeTag.Int64:
                    case TypeTag.TimeTag:
                    {
                        offset += 8;
                        break;
                    }
                    case TypeTag.String:
                    case TypeTag.AltTypeString:
                    {
                        if (!TryGetStringLength(ElementPtr + offset, out var length))
                        {
                            return false;
                        }
                        offset += length.AlignNext4();
                        break;
                    }
                    case TypeTag.Blob:
                    {
                        var dataOffset = offset + 4;
                        if (dataOffset > ElementSize)
                        {
                            return false;
                        }
                        offset = dataOffset + ReadInt32Unchecked(offset).Align4();
                        break;
                    }
                }
            }

            return offset <= ElementSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckIndexValid(int index)
        {
            if (index < 0 || index >= ArgumentCount)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index ({index}) must be non-negative and be less than the argument count ({ArgumentCount}).");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryGetStringLength(byte* str, out int length)
        {
            var endPtr = ElementPtr + ElementSize;

            for (var ptr = str; ptr < endPtr; ptr++)
            {
                if (*ptr == 0)
                {
                    length = (int)(ptr - str);
                    return true;
                }
            }

            length = default;
            return false;
        }
    }
}
