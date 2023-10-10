using System;
using System.Runtime.InteropServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to parse and access the contents of OSC Packets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An OSC Packet is the unit of transmission of OSC. It may contain either an OSC Message or an OSC Bundle.
    /// </para>
    /// <para>
    /// This parses and reads arguments from the packet directly from the original message buffer in order to minimize
    /// GC allocations and achieve high performance.
    /// </para>
    /// </remarks>
    public unsafe class OscPacket : IDisposable
    {
        GCHandle m_BufferHandle;
        OscMessage m_Message;
        OscBundle m_Bundle;

        /// <summary>
        /// The buffer the OSC Packet is stored in.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// A pointer to the start of the packet buffer.
        /// </summary>
        public byte* BufferPtr { get; }

        /// <summary>
        /// The parsed contents of the packet.
        /// </summary>
        /// <remarks>
        /// Use this after calling <see cref="Parse"/> so that new data is available to read.
        /// Any reference to the contents should be considered invalidated by any subsequent call to <see cref="Parse"/>.
        /// </remarks>
        public OscBundleElement RootElement { get; private set; }

        /// <summary>
        /// Create a new <see cref="OscPacket"/> instance.
        /// </summary>
        /// <param name="buffer">The buffer to read the OSC Packet from.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is null.</exception>
        public OscPacket(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Buffer = buffer;
            BufferPtr = OscUtils.PinPtr<byte, byte>(buffer, out m_BufferHandle);

            m_Message = new OscMessage(BufferPtr);
            m_Bundle = new OscBundle(BufferPtr);
        }

        /// <summary>
        /// Disposes this instance in case it was not properly disposed.
        /// </summary>
        ~OscPacket()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            m_BufferHandle.SafeFree();
        }

        /// <summary>
        /// Parse a contents of the OSC Packet buffer.
        /// </summary>
        /// <remarks>
        /// This must be called before accessing the parsed packet using <see cref="RootElement"/>.
        /// From the moment this is called until the packet contents are no longer needed, the buffer containing the original packet must not be changed, or else the
        /// the read data will be corrupted.
        /// </remarks>
        /// <param name="size">The length of the packet in bytes.</param>
        /// <param name="offset">The index of the byte in the buffer to start reading from.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="size"/> or <paramref name="offset"/> is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="size"/> and <paramref name="offset"/> combined exceed
        /// the length of the packet buffer.</exception>
        public void Parse(int size, int offset = 0)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Cannot be negative.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Cannot be negative.");
            if (offset + size > Buffer.Length)
                throw new ArgumentException($"The offset ({offset}) + size ({size}) exceeds buffer size ({Buffer.Length})");

            if (size >= 8 && OscBundleElement.IsBundleTagAtIndex(BufferPtr + offset))
            {
                RootElement = m_Bundle;
            }
            else
            {
                RootElement = m_Message;
            }

            RootElement.Parse(size, offset);
        }
    }
}
