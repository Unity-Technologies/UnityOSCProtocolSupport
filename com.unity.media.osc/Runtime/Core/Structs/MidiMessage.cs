using System;
using System.Runtime.InteropServices;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A struct that represents a Musical Instrument Digital Interface (MIDI) message.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct MidiMessage : IEquatable<MidiMessage>
    {
        [FieldOffset(0)]
        readonly int m_Data;

        /// <summary>
        /// The MIDI port ID.
        /// </summary>
        [FieldOffset(0)]
        public readonly byte PortId;

        /// <summary>
        /// The type of the message.
        /// </summary>
        [FieldOffset(1)]
        public readonly byte Status;

        /// <summary>
        /// The first byte of data in the message.
        /// </summary>
        [FieldOffset(2)]
        public readonly byte Data1;

        /// <summary>
        /// The second byte of data in the message.
        /// </summary>
        [FieldOffset(3)]
        public readonly byte Data2;

        /// <summary>
        /// Creates a new <see cref="MidiMessage"/> instance.
        /// </summary>
        /// <param name="portId">The MIDI port ID.</param>
        /// <param name="status">The type of the message.</param>
        /// <param name="data1">The first byte of data in the message.</param>
        /// <param name="data2">The second byte of data in the message.</param>
        public MidiMessage(byte portId, byte status, byte data1, byte data2)
        {
            m_Data = 0;
            PortId = portId;
            Status = status;
            Data1 = data1;
            Data2 = data2;
        }

        /// <summary>
        /// Reads a <see cref="MidiMessage"/> instance from a buffer.
        /// </summary>
        /// <param name="bytes">The buffer to read from.</param>
        /// <param name="offset">The index in the buffer to start reading from.</param>
        public MidiMessage(byte[] bytes, int offset)
        {
            m_Data = 0;
            PortId = bytes[offset];
            Status = bytes[offset + 1];
            Data1 = bytes[offset + 2];
            Data2 = bytes[offset + 3];
        }

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>A string that represents the current instance.</returns>
        public override string ToString()
        {
            return $"Port ID: {PortId}, Status: {Status}, Data 1: {Data1}, Data 2: {Data2}";
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="MidiMessage"/>.
        /// </summary>
        /// <param name="other">A value to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="other"/> has the same value as this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(MidiMessage other)
        {
            return m_Data == other.m_Data;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="MidiMessage"/> and equals
        /// the value of this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is MidiMessage other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return m_Data.GetHashCode();
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="MidiMessage"/> are equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(MidiMessage a, MidiMessage b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="MidiMessage"/> are not equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> do not represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(MidiMessage a, MidiMessage b)
        {
            return !a.Equals(b);
        }
    }
}

