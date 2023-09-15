using System;
using System.Runtime.CompilerServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A struct representing an OSC Time Tag.
    /// </summary>
    /// <remarks>
    /// OSC Time Tags contain a 64-bit NTP timestamp, described in RFC 5905.
    /// </remarks>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc5905"/>
    public readonly struct TimeTag : IComparable, IComparable<TimeTag>, IEquatable<TimeTag>
    {
        /// <summary>
        /// A special value of time tag that indicates the present moment in time.
        /// </summary>
        public static TimeTag Now { get; } = new TimeTag(0, 1);

        /// <summary>
        /// The date from which time starts in the NTP date format.
        /// </summary>
        static readonly DateTime k_Epoch = new DateTime(1900, 1, 1);

        /// <summary>
        /// The number of ticks per second used by <see cref="DateTime"/>.
        /// </summary>
        const ulong k_TicksPerSecond = 1000UL * TimeSpan.TicksPerMillisecond;

        /// <summary>
        /// The number of seconds since the last epoch.
        /// </summary>
        public readonly uint Seconds;

        /// <summary>
        /// The number of 1/(2^32) fractions of a second elapsed this second.
        /// </summary>
        public readonly uint Fraction;

        /// <summary>
        /// Creates a new <see cref="TimeTag"/> instance.
        /// </summary>
        /// <param name="seconds">The number of seconds since the last epoch.</param>
        /// <param name="fraction">The number of ~200 picosecond fractions of a second elapsed this second.</param>
        public TimeTag(uint seconds, uint fraction)
        {
            Seconds = seconds;
            Fraction = fraction;
        }

        /// <summary>
        /// Creates a new <see cref="TimeTag"/> instance.
        /// </summary>
        /// <param name="time">The time to represent.</param>
        public TimeTag(DateTime time)
        {
            var ticks = (ulong)(time.Ticks - k_Epoch.Ticks);
            Seconds = (uint)(ticks / k_TicksPerSecond);
            Fraction = (uint)(((ticks % k_TicksPerSecond) << 32) / k_TicksPerSecond);
        }

        /// <summary>
        /// Reads a <see cref="TimeTag"/> instance from a buffer with big-endian bytes.
        /// </summary>
        /// <param name="bytes">The buffer to read from.</param>
        /// <param name="offset">The index in the buffer to start reading from.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if reading at <paramref name="offset"/> would overflow the buffer.</exception>
        /// <returns>The read <see cref="TimeTag"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TimeTag FromBigEndianBytes(byte[] bytes, int offset)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Cannot be negative.");
            if (offset + sizeof(TimeTag) > bytes.Length)
                throw new ArgumentException($"Reading at offset {offset} overflows the byte buffer.");

            fixed (byte* ptr = &bytes[offset])
            {
                return FromBigEndianBytes((uint*)ptr);
            }
        }

        /// <summary>
        /// Reads a <see cref="TimeTag"/> instance from a buffer with big-endian bytes.
        /// </summary>
        /// <remarks>
        /// It is up to the caller to ensure the buffer pointer is valid to read from.
        /// </remarks>
        /// <param name="bytes">The buffer to read from.</param>
        /// <returns>The read <see cref="TimeTag"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TimeTag FromBigEndianBytes(byte* bytes)
        {
            return FromBigEndianBytes((uint*)bytes);
        }

        static unsafe TimeTag FromBigEndianBytes(uint* ptr)
        {
            var seconds = ptr[0].FromBigEndian();
            var fractions = ptr[1].FromBigEndian();
            return new TimeTag(seconds, fractions);
        }

        /// <summary>
        /// Gets the time as a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>The time represented by this instance.</returns>
        public DateTime ToDateTime()
        {
            if (Equals(Now))
            {
                return DateTime.Now;
            }

            var ticks = (Seconds * k_TicksPerSecond) + ((Fraction * k_TicksPerSecond) >> 32);
            return new DateTime(k_Epoch.Ticks + (long)ticks);
        }

        /// <summary>
        /// Writes this value to a buffer in big-endian bytes.
        /// </summary>
        /// <param name="bytes">The buffer to write to.</param>
        /// <param name="offset">The index in the buffer to start writing from.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if writing at <paramref name="offset"/> would overflow the buffer.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToBigEndianBytes(byte[] bytes, int offset)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Cannot be negative.");
            if (offset + sizeof(TimeTag) > bytes.Length)
                throw new ArgumentException($"Writing at offset {offset} overflows the byte buffer.");

            fixed (byte* ptr = &bytes[offset])
            {
                ToBigEndianBytes((uint*)ptr);
            }
        }

        /// <summary>
        /// Writes this value to a buffer in big-endian bytes.
        /// </summary>
        /// <remarks>
        /// It is up to the caller to ensure the buffer pointer is valid to write to.
        /// </remarks>
        /// <param name="bytes">The buffer to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToBigEndianBytes(uint* bytes)
        {
            bytes[0] = Seconds.ToBigEndian();
            bytes[1] = Fraction.ToBigEndian();
        }

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>A string that represents the current instance.</returns>
        public override string ToString()
        {
            return ToDateTime().ToString();
        }

        /// <summary>
        /// Compares this instance to a specified <see cref="TimeTag"/> and returns an indication of their relative values.
        /// </summary>
        /// <param name="other">The value to compare with this instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and <paramref name="other"/>.
        /// <br/>* Returns a negative value when this instance is less than <paramref name="other"/>.
        /// <br/>* Returns zero when this instance is the same as <paramref name="other"/>.
        /// <br/>* Returns a positive value when this instance is greater than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(TimeTag other)
        {
            var secondsComparison = Seconds.CompareTo(other.Seconds);
            return secondsComparison != 0 ? secondsComparison : Fraction.CompareTo(other.Fraction);
        }

        /// <summary>
        /// Compares this instance to a specified object and returns an indication of their relative values.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>A signed number indicating the relative values of this instance and <paramref name="obj"/>.
        /// <br/>* Returns a negative value when <paramref name="obj"/> is not a valid <see cref="TimeTag"/> instance or this instance is less than <paramref name="obj"/>.
        /// <br/>* Returns zero when this instance is the same as <paramref name="obj"/>.
        /// <br/>* Returns a positive value when this instance is greater than <paramref name="obj"/>.
        /// </returns>
        public int CompareTo(object obj)
        {
            return obj is TimeTag other ? CompareTo(other) : -1;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="TimeTag"/>.
        /// </summary>
        /// <param name="other">A value to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="other"/> has the same value as this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(TimeTag other)
        {
            return Seconds == other.Seconds && Fraction == other.Fraction;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="TimeTag"/> and equals
        /// the value of this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is TimeTag other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Seconds * 397) ^ (int)Fraction;
            }
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="TimeTag"/> are equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(TimeTag a, TimeTag b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="TimeTag"/> are not equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> do not represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(TimeTag a, TimeTag b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Determines whether one specified <see cref="TimeTag"/> is later than or the same as another specified <see cref="TimeTag"/>.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is later than or the same as <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(TimeTag a, TimeTag b)
        {
            return a.CompareTo(b) >= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="TimeTag"/> is earlier than or the same as another specified <see cref="TimeTag"/>.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is earlier than or the same as <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(TimeTag a, TimeTag b)
        {
            return a.CompareTo(b) <= 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="TimeTag"/> is later than another specified <see cref="TimeTag"/>.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is later than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(TimeTag a, TimeTag b)
        {
            return a.CompareTo(b) > 0;
        }

        /// <summary>
        /// Determines whether one specified <see cref="TimeTag"/> is earlier than another specified <see cref="TimeTag"/>.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is earlier than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(TimeTag a, TimeTag b)
        {
            return a.CompareTo(b) < 0;
        }
    }
}
