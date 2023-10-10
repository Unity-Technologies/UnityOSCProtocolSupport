using System;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class that contains one or many <see cref="OscBundleElement"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An OSC Bundle contains a group of bundle elements that are either an OSC Message or OSC Bundle.
    /// Note that bundles can be recursive. They also contain a <see cref="TimeTag"/> which indicates
    /// when the callbacks for the contained OSC Messages should be invoked.
    /// </para>
    /// <para>
    /// A bundle is identified by the presence of the <see cref="OscConstants.BundlePrefix"/> OSC String
    /// at the first byte of the bundle contents.
    /// </para>
    /// </remarks>
    public sealed unsafe class OscBundle : OscBundleElement
    {
        OscMessage[] m_Messages;
        OscBundle[] m_Bundles;

        /// <summary>
        /// The number of messages in the bundle.
        /// </summary>
        public int MessageCount { get; private set; }

        /// <summary>
        /// The number of nested bundles in the bundle.
        /// </summary>
        public int BundleCount { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OscBundle"/> instance.
        /// </summary>
        /// <param name="bufferPtr">The buffer containing the bundle element.</param>
        /// <param name="capacity">The initial capacity of the bundle element containers.</param>
        internal OscBundle(byte* bufferPtr, int capacity = 4) : base(bufferPtr)
        {
            m_Messages = new OscMessage[capacity];
            m_Bundles = new OscBundle[0];

            for (var i = 0; i < capacity; i++)
            {
                m_Messages[i] = new OscMessage(m_BufferPtr);
            }
        }

        /// <summary>
        /// The time at which the OSC Messages in the bundle should have their callbacks invoked.
        /// </summary>
        /// <remarks>
        /// If the time is in future, the bundle should be stored until the callbacks can be invoked at the specified time.
        /// If the time has already lapsed, the callbacks should be invoked as soon as possible.
        /// </remarks>
        /// <returns>
        /// The time tag of the bundle. If the returned time is equal to <see cref="TimeTag.Now"/>
        /// the message callbacks should be immediately invoked.
        /// </returns>
        public TimeTag GetTimeTag()
        {
            // The time tag follows the bundle prefix
            return TimeTag.FromBigEndianBytes(ElementPtr + 8);
        }

        /// <summary>
        /// Gets a message by index.
        /// </summary>
        /// <param name="index">The index of the message to get.</param>
        /// <returns>The message at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the number of messages.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public OscMessage GetMessage(int index)
        {
            if (index < 0 || index >= MessageCount)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index ({index}) must be non-negative and be less than the message count ({MessageCount}).");

            return m_Messages[index];
        }

        /// <summary>
        /// Gets a nested bundle by index.
        /// </summary>
        /// <param name="index">The index of the bundle to get.</param>
        /// <returns>The bundle at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the number of nested bundles.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public OscBundle GetBundle(int index)
        {
            if (index < 0 || index >= BundleCount)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index ({index}) must be non-negative and be less than the bundle count ({BundleCount}).");

            return m_Bundles[index];
        }

        /// <inheritdoc/>
        protected override bool OnParse()
        {
            MessageCount = 0;
            BundleCount = 0;

            // skip the bundle prefix and time tag
            var offset = 16;

            if (offset > ElementSize)
            {
                return false;
            }

            while (offset < ElementSize)
            {
                var elementSizePtr = ElementPtr + offset;

                // read the element size
                var size = (*(int*)elementSizePtr).FromBigEndian();

                if (size < 0)
                {
                    return false;
                }

                var elementContentPtr = elementSizePtr + sizeof(int);
                var elementOffset = ElementOffset + offset + sizeof(int);

                // check if the element is a bundle or message
                if (IsBundleTagAtIndex(elementContentPtr))
                {
                    var oldCapacity = m_Bundles.Length;

                    if (OscUtils.EnsureCapacity(ref m_Bundles, BundleCount + 1))
                    {
                        for (var i = oldCapacity; i < m_Bundles.Length; i++)
                        {
                            m_Bundles[i] = new OscBundle(m_BufferPtr);
                        }
                    }

                    var bundle = m_Bundles[BundleCount++];
                    bundle.Parse(size, elementOffset);
                }
                else
                {
                    var oldCapacity = m_Messages.Length;

                    if (OscUtils.EnsureCapacity(ref m_Messages, MessageCount + 1))
                    {
                        for (var i = oldCapacity; i < m_Messages.Length; i++)
                        {
                            m_Messages[i] = new OscMessage(m_BufferPtr);
                        }
                    }

                    var message = m_Messages[MessageCount++];
                    message.Parse(size, elementOffset);
                }

                offset += sizeof(int) + size;
            }

            return true;
        }
    }
}
