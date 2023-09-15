using System;
using System.IO;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to write an OSC stream that uses a packet length prefix.
    /// </summary>
    /// <remarks>
    /// A packet length prefix is used by OSC streams following the OSC 1.0 specification. It is not suitable for
    /// unreliable streams as this approach makes error recovery impractical.
    /// </remarks>
    /// <seealso cref="SlipStreamWriter"/>
    public class LengthPrefixStreamWriter : OscStreamWriter
    {
        readonly byte[] m_PrefixBuffer = new byte[sizeof(int)];

        /// <summary>
        /// Creates a new <see cref="LengthPrefixStreamWriter"/> instance.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public LengthPrefixStreamWriter(Stream stream) : base(stream)
        {
        }

        /// <inheritdoc />
        public override void WriteToStream(byte[] packetBuffer, int packetLength)
        {
            if (packetBuffer == null || packetLength <= 0)
            {
                return;
            }
            if (!BitConverter.TryWriteBytes(m_PrefixBuffer.AsSpan(), packetLength.ToBigEndian()))
            {
                return;
            }

            Stream.Write(m_PrefixBuffer);
            Stream.Write(packetBuffer, 0, packetLength);
        }
    }
}
