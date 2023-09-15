using System;
using System.IO;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to write an OSC stream that frames packets using SLIP (RFC 1055).
    /// </summary>
    /// <remarks>
    /// SLIP is used by OSC streams following the OSC 1.1 specification. It is suitable for both
    /// reliable and unreliable streams since it is able to recover from transmission errors.
    /// </remarks>
    /// <seealso cref="LengthPrefixStreamWriter"/>
    public class SlipStreamWriter : OscStreamWriter
    {
        byte[] m_EncodeBuffer;
        int m_EncodeSize;

        /// <summary>
        /// Creates a new <see cref="SlipStreamWriter"/> instance.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public SlipStreamWriter(Stream stream) : base(stream)
        {
            m_EncodeBuffer = new byte[4096];
        }

        /// <inheritdoc />
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public override void WriteToStream(byte[] packetBuffer, int packetLength)
        {
            if (packetBuffer == null || packetLength <= 0)
            {
                return;
            }

            // First encode the message into a buffer to avoid frequently writing into the stream.
            // This improves performance for some types of streams.

            // Ensure the encoding buffer can fit the entire message. Escaped bytes use twice the space,
            // so the encoded message can be at most double the size, with two additional bytes for the
            // END bytes at the start and end of the message.
            var maxEncodedSize = (2 * packetLength) + 2;

            if (m_EncodeBuffer.Length < maxEncodedSize)
            {
                m_EncodeBuffer = new byte[maxEncodedSize];
            }

            // clear the encoding buffer
            m_EncodeSize = 0;

            // Always start with an END character to flush any line noise. This is not needed for reliable
            // transport, but helps error recovery over unreliable transport.
            m_EncodeBuffer[m_EncodeSize++] = SlipConstants.End;

            // write the packet bytes escaping bytes if needed
            for (var i = 0; i < packetLength; i++)
            {
                var value = packetBuffer[i];

                switch (value)
                {
                    case SlipConstants.End:
                    {
                        m_EncodeBuffer[m_EncodeSize++] = SlipConstants.Esc;
                        m_EncodeBuffer[m_EncodeSize++] = SlipConstants.EscEnd;
                        break;
                    }
                    case SlipConstants.Esc:
                    {
                        m_EncodeBuffer[m_EncodeSize++] = SlipConstants.Esc;
                        m_EncodeBuffer[m_EncodeSize++] = SlipConstants.EscEsc;
                        break;
                    }
                    default:
                    {
                        m_EncodeBuffer[m_EncodeSize++] = value;
                        break;
                    }
                }
            }

            // tell the receiver that the packet is done
            m_EncodeBuffer[m_EncodeSize++] = SlipConstants.End;

            // write the packet into the stream
            Stream.Write(m_EncodeBuffer, 0, m_EncodeSize);
        }
    }
}
