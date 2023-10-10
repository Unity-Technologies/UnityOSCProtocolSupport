using System;
using System.IO;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to read an OSC stream that frames packets using SLIP (RFC 1055).
    /// </summary>
    /// <remarks>
    /// SLIP is used by OSC streams following the OSC 1.1 specification. It is suitable for both
    /// reliable and unreliable streams since it is able to recover from transmission errors.
    /// </remarks>
    /// <seealso cref="LengthPrefixStreamReader"/>
    public class SlipStreamReader : OscStreamReader
    {
        byte[] m_DecodeBuffer;
        OscPacket m_Packet;
        int m_BytesRead;
        bool m_Escape;

        /// <summary>
        /// Creates a new <see cref="SlipStreamReader"/> instance.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="packetHandler">The callback invoked when a packet is read from the stream. The first argument is the unparsed packet, and the
        /// second argument is the length of the packet in bytes.</param>
        public SlipStreamReader(Stream stream, Action<OscPacket, int> packetHandler) : base(stream, packetHandler)
        {
            m_DecodeBuffer = new byte[4096];
            m_Packet = new OscPacket(new byte[4096]);

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
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public override void ReadAllPackets()
        {
            while (true)
            {
                // First read the stream into a buffer to avoid frequently reading from the stream.
                // This improves performance for some types of streams.
                var bytesRead = Stream.Read(m_DecodeBuffer, 0, m_DecodeBuffer.Length);

                if (bytesRead <= 0)
                {
                    return;
                }

                for (var i = 0; i < bytesRead; i++)
                {
                    var value = m_DecodeBuffer[i];

                    // handle special bytes
                    switch (value)
                    {
                        case SlipConstants.End:
                        {
                            if (m_BytesRead > 0)
                            {
                                PacketHandler.Invoke(m_Packet, m_BytesRead);
                            }

                            Reset();
                            continue;
                        }
                        case SlipConstants.Esc:
                        {
                            m_Escape = true;
                            continue;
                        }
                        case SlipConstants.EscEnd:
                        {
                            if (m_Escape)
                            {
                                value = SlipConstants.End;
                            }
                            break;
                        }
                        case SlipConstants.EscEsc:
                        {
                            if (m_Escape)
                            {
                                value = SlipConstants.Esc;
                            }
                            break;
                        }
                    }

                    // ensure the packet buffer is large enough to fit the received packet
                    if (m_BytesRead >= m_Packet.Buffer.Length)
                    {
                        var buffer = new byte[2 * m_Packet.Buffer.Length];
                        Buffer.BlockCopy(m_Packet.Buffer, 0, buffer, 0, m_BytesRead);

                        m_Packet.Dispose();
                        m_Packet = new OscPacket(buffer);
                    }

                    // write the character into the packet buffer
                    m_Packet.Buffer[m_BytesRead++] = value;
                    m_Escape = false;
                }
            }
        }

        void Reset()
        {
            m_BytesRead = 0;
            m_Escape = false;
        }
    }
}
