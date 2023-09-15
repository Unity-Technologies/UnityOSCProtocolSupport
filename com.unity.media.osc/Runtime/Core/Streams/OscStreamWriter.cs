using System;
using System.IO;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class used to write an OSC stream.
    /// </summary>
    public abstract class OscStreamWriter : IDisposable
    {
        /// <summary>
        /// The stream to write OSC packets to.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Creates a new <see cref="OscStreamWriter"/> instance.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="stream"/> is not writable.</exception>
        protected OscStreamWriter(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException("The stream must be writable.", nameof(stream));
            }

            Stream = stream;
        }

        /// <summary>
        /// Disposes this instance in case it was not properly disposed.
        /// </summary>
        ~OscStreamWriter()
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
        /// Writes an OSC packet into the stream.
        /// </summary>
        /// <param name="packetBuffer">A buffer containing an OSC Packet.</param>
        /// <param name="packetLength">The length of the packet in bytes.</param>
        public abstract void WriteToStream(byte[] packetBuffer, int packetLength);
    }
}
