using System;
using System.IO;

namespace Unity.Media.Osc
{
    /// <summary>
    /// An exception used when <see cref="LengthPrefixStreamReader"/> encounters a packet length that is
    /// negative or is larger than the configured maximum packet size.
    /// </summary>
    public class InvalidPacketSizeException : Exception
    {
        /// <summary>
        /// The packet length read from the stream.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Creates a new <see cref="InvalidPacketSizeException"/> instance.
        /// </summary>
        /// <param name="size">The packet length read from the stream.</param>
        /// <param name="message">The exception message.</param>
        internal InvalidPacketSizeException(int size, string message) : base(message)
        {
            Size = size;
        }
    }

    /// <summary>
    /// A class used to read an OSC stream that uses a packet length prefix.
    /// </summary>
    /// <remarks>
    /// A packet length prefix is used by OSC streams following the OSC 1.0 specification. It is not suitable for
    /// unreliable streams as this approach makes error recovery impractical.
    /// </remarks>
    /// <seealso cref="SlipStreamReader"/>
    public class LengthPrefixStreamReader : OscStreamReader
    {
        const int k_DefaultMaxPacketSize = 10 * 1024 * 1024;

        readonly byte[] m_PrefixBuffer = new byte[sizeof(int)];
        readonly int m_MaxPacketSize;
        OscPacket m_Packet;
        int m_PacketSize;
        int m_PrefixBytesRead;
        int m_PacketBytesRead;

        /// <summary>
        /// Creates a new <see cref="LengthPrefixStreamReader"/> instance.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="packetHandler">The callback invoked when a packet is read from the stream. The first argument is the unparsed packet, and the
        /// second argument is the length of the packet in bytes.</param>
        /// <param name="maxPacketSize">The size of the largest packet that can be read from the stream in bytes.</param>
        public LengthPrefixStreamReader(Stream stream, Action<OscPacket, int> packetHandler, int maxPacketSize = k_DefaultMaxPacketSize)
            : base(stream, packetHandler)
        {
            m_Packet = new OscPacket(new byte[4096]);
            m_MaxPacketSize = Math.Max(maxPacketSize, 0);

            Reset();
        }

        /// <inheritdoc />
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (m_Packet != null)
            {
                m_Packet.Dispose();
                m_Packet = null;
            }
        }

        /// <inheritdoc />
        public override void ReadAllPackets()
        {
            while (true)
            {
                // read the length prefix
                if (m_PrefixBytesRead < m_PrefixBuffer.Length)
                {
                    var count = Stream.Read(m_PrefixBuffer, m_PrefixBytesRead, m_PrefixBuffer.Length - m_PrefixBytesRead);

                    if (count <= 0)
                    {
                        return;
                    }

                    m_PrefixBytesRead += count;

                    // once the entire prefix is read get the packet length
                    if (m_PrefixBytesRead == m_PrefixBuffer.Length)
                    {
                        m_PacketSize = BitConverter.ToInt32(m_PrefixBuffer).FromBigEndian();

                        if (m_PacketSize < 0 || m_PacketSize > m_MaxPacketSize)
                        {
                            throw new InvalidPacketSizeException(m_PacketSize, $"Packet length ({m_PacketSize}) is outside of the allowed range [0, {m_MaxPacketSize}]");
                        }

                        if (m_PacketSize > m_Packet.Buffer.Length)
                        {
                            m_Packet.Dispose();
                            m_Packet = new OscPacket(new byte[2 * m_PacketSize]);
                        }
                    }
                }

                // read the packet body
                if (m_PacketSize > 0)
                {
                    var count = Stream.Read(m_Packet.Buffer, m_PacketBytesRead, m_PacketSize - m_PacketBytesRead);

                    if (count <= 0)
                    {
                        return;
                    }

                    m_PacketBytesRead += count;

                    // once the entire packet is read invoke the callback to handle the packet
                    if (m_PacketBytesRead == m_PacketSize)
                    {
                        PacketHandler.Invoke(m_Packet, m_PacketBytesRead);
                        Reset();
                    }
                }
                else
                {
                    Reset();
                }
            }
        }

        void Reset()
        {
            m_PacketSize = int.MinValue;
            m_PrefixBytesRead = 0;
            m_PacketBytesRead = 0;
        }
    }
}
