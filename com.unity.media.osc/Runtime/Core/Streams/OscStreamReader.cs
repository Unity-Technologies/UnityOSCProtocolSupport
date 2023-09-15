using System;
using System.IO;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to read an OSC stream.
    /// </summary>
    public abstract class OscStreamReader : IDisposable
    {
        /// <summary>
        /// The stream to read OSC packets from.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// The callback invoked when a packet is read from the stream.
        /// </summary>
        /// <remarks>
        /// The first argument is the unparsed packet and the second argument is the length of the packet in bytes.
        /// </remarks>
        protected Action<OscPacket, int> PacketHandler { get; }

        /// <summary>
        /// Creates a new <see cref="OscStreamReader"/> instance.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="packetHandler">The callback invoked when a packet is read from the stream. The first argument is the unparsed packet and the
        /// second argument is the length of the packet in bytes.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> or <paramref name="packetHandler"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is not readable.</exception>
        protected OscStreamReader(Stream stream, Action<OscPacket, int> packetHandler)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (packetHandler == null)
            {
                throw new ArgumentNullException(nameof(packetHandler));
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            }

            Stream = stream;
            PacketHandler = packetHandler;
        }

        /// <summary>
        /// Disposes this instance in case it was not properly disposed.
        /// </summary>
        ~OscStreamReader()
        {
            OnDispose(false);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            OnDispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources held by this instance.
        /// </summary>
        /// <param name="disposing">This is <see langword="true"/> when <see cref="Dispose"/> was called, and <see langword="false"/>
        /// when the instance is being disposed on the finalizer thread.</param>
        protected virtual void OnDispose(bool disposing)
        {
            Stream.Dispose();
        }

        /// <summary>
        /// Reads all available OSC packets from the stream.
        /// </summary>
        public abstract void ReadAllPackets();
    }
}
