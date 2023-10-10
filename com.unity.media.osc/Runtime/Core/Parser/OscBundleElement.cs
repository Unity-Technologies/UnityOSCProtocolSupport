using System.Runtime.CompilerServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The base class for both <see cref="OscMessage"/> and <see cref="OscBundle"/>.
    /// </summary>
    /// <remarks>
    /// An OSC Bundle Element consists of either an OSC Message or OSC Bundle.
    /// </remarks>
    public abstract unsafe class OscBundleElement
    {
        /// <summary>
        /// The buffer containing the bundle element.
        /// </summary>
        protected readonly byte* m_BufferPtr;

        /// <summary>
        /// Checks if this bundle element was successfully parsed.
        /// </summary>
        /// <remarks>
        /// If this is <see langword="false"/>, you should not attempt to access the contents
        /// of this element, they are undefined.
        /// </remarks>
        public bool IsValid { get; private set; }

        /// <summary>
        /// The length of the bundle element in bytes.
        /// </summary>
        public int ElementSize { get; private set; }

        /// <summary>
        /// The index of the byte in the buffer at which the bundle element starts.
        /// </summary>
        internal int ElementOffset { get; private set; }

        /// <summary>
        /// The start of the bundle element.
        /// </summary>
        protected byte* ElementPtr { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OscBundleElement"/> instance.
        /// </summary>
        /// <param name="bufferPtr">The buffer containing the bundle element.</param>
        internal OscBundleElement(byte* bufferPtr)
        {
            m_BufferPtr = bufferPtr;
        }

        /// <summary>
        /// Parse the contents of a bundle element.
        /// </summary>
        /// <param name="size">The length of the bundle element in bytes.</param>
        /// <param name="offset">The index of the byte in the buffer at which the bundle element starts.</param>
        internal void Parse(int size, int offset)
        {
            ElementSize = size;
            ElementOffset = offset;
            ElementPtr = m_BufferPtr + offset;

            IsValid = OnParse();
        }

        /// <summary>
        /// Called to parse the bundle element.
        /// </summary>
        /// <returns><see langword="true"/> if the element was successfully parsed; otherwise, <see langword="false"/>.</returns>
        protected abstract bool OnParse();

        /// <summary>
        /// Check if <see cref="OscConstants.BundlePrefix"/> is present at the specified bytes.
        /// </summary>
        /// <param name="bytes">The buffer to check for the bundle prefix.</param>
        /// <returns><see langword="true"/> if a bundle starts at the given bytes; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBundleTagAtIndex(byte* bytes)
        {
            return *(long*)bytes == OscConstants.BundlePrefixLong;
        }
    }
}
