namespace Unity.Media.Osc
{
    static class SlipConstants
    {
        /// <summary>
        /// The byte used to indicate the end of a packet.
        /// </summary>
        public const byte End = 0xC0;

        /// <summary>
        /// The byte used to indicate the next byte of data is escaped.
        /// </summary>
        public const byte Esc = 0xDB;

        /// <summary>
        /// The escaped byte used to indicate a data byte with the same numeric value as <see cref="End"/>.
        /// </summary>
        public const byte EscEnd = 0xDC;

        /// <summary>
        /// The escaped byte used to indicate a data byte with the same numeric value as <see cref="Esc"/>.
        /// </summary>
        public const byte EscEsc = 0xDD;
    }
}
