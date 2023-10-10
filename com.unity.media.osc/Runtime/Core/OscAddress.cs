using System;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A struct that represents an OSC Address or OSC Address Pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An OSC Address consists of an ASCII string which is divided into parts
    /// by forward slash (/) characters. The last part is called the OSC Method,
    /// with all previous parts called OSC Containers.
    /// </para>
    /// <para>
    /// An OSC Address Pattern is similar to an address, but supports additional characters
    /// used for pattern matching, thus allowing a single pattern to match several addresses.
    /// </para>
    /// <para>
    /// This implementation does not use managed strings in order to reduce GC allocations.
    /// The address string is always followed by a null terminator.
    /// </para>
    /// </remarks>
    public readonly unsafe struct OscAddress : IDisposable, IEquatable<OscAddress>
    {
        readonly NativeArray<byte> m_Array;

        /// <summary>
        /// A pointer to the start of the string.
        /// </summary>
        public byte* Pointer { get; }

        /// <summary>
        /// The number of characters in the string.
        /// </summary>
        /// <remarks>
        /// This does not include the null terminator.
        /// </remarks>
        public int Length { get; }

        /// <summary>
        /// The type of address this string represents.
        /// </summary>
        public AddressType Type { get; }

        /// <summary>
        /// Creates a new <see cref="OscAddress"/> instance.
        /// </summary>
        /// <remarks>
        /// The address bytes are copied from the given string into new native memory.
        /// </remarks>
        /// <param name="address">The address to store.</param>
        /// <param name="allocator">The native allocator to use for the string.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is null.</exception>
        public OscAddress(string address, Allocator allocator = Allocator.Persistent)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            // allocate string buffer with room for null terminator
            Length = Encoding.ASCII.GetByteCount(address);
            m_Array = new NativeArray<byte>(Length + 1, allocator);
            Pointer = (byte*)m_Array.GetUnsafePtr();

            // copy the string contents
            fixed (char* addressPtr = address)
            {
                Encoding.ASCII.GetBytes(addressPtr, address.Length, Pointer, Length);
            }

            // add the null terminator
            Pointer[Length] = 0;

            Type = OscUtils.GetAddressType(Pointer, Length);
        }

        /// <summary>
        /// Creates a new <see cref="OscAddress"/> instance.
        /// </summary>
        /// <remarks>
        /// The address bytes are copied from the given address into new native memory.
        /// </remarks>
        /// <param name="address">The address to store.</param>
        /// <param name="allocator">The native allocator to use for the string.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is null.</exception>
        public OscAddress(in OscAddress address, Allocator allocator)
        {
            // allocate string buffer with room for null terminator
            m_Array = new NativeArray<byte>(address.Length + 1, allocator);
            Pointer = (byte*)m_Array.GetUnsafePtr();
            Length = address.Length;

            // copy the string contents
            UnsafeUtility.MemCpy(Pointer, address.Pointer, Length);

            // add the null terminator
            Pointer[Length] = 0;

            Type = address.Type;
        }

        /// <summary>
        /// Creates a new <see cref="OscAddress"/> instance.
        /// </summary>
        /// <remarks>
        /// The address bytes are copied from the given buffer into new native memory.
        /// </remarks>
        /// <param name="addressPtr">A pointer to the address string to store. Must be ASCII encoded. It is up to
        /// the caller to ensure the address is encoded correctly.</param>
        /// <param name="length">The length of the string in bytes.</param>
        /// <param name="allocator">The native allocator to use for the string.</param>
        public OscAddress(byte* addressPtr, int length, Allocator allocator)
        {
            // allocate string buffer with room for null terminator
            m_Array = new NativeArray<byte>(length + 1, allocator);
            Pointer = (byte*)m_Array.GetUnsafePtr();
            Length = length;

            // copy the string contents
            UnsafeUtility.MemCpy(Pointer, addressPtr, Length);

            // add the null terminator
            Pointer[Length] = 0;

            Type = OscUtils.GetAddressType(Pointer, Length);
        }

        /// <summary>
        /// Creates a new <see cref="OscAddress"/> instance.
        /// </summary>
        /// <remarks>
        /// The address bytes are not copied, instead this references the given string buffer. The address instance
        /// should be disposed before modifying the contents of the referenced buffer.
        /// </remarks>
        /// <param name="addressPtr">A pointer to the address string to store. Must be ASCII encoded with a null
        /// terminator. It is up to the caller to ensure the address is encoded correctly.</param>
        /// <param name="length">The length of the string in bytes, excluding the null terminator.</param>
        public OscAddress(byte* addressPtr, int length)
        {
            m_Array = default;
            Pointer = addressPtr;
            Length = length;

            Type = OscUtils.GetAddressType(Pointer, Length);
        }

        /// <summary>
        /// Releases the native memory held by this instance.
        /// </summary>
        public void Dispose()
        {
            if (m_Array.IsCreated)
            {
                m_Array.Dispose();
            }
        }

        /// <summary>
        /// Evaluate if this address matches another.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An OSC Address Pattern matches an OSC Address if the OSC Address and the OSC Address Pattern contain the same
        /// number of parts; and each part of the OSC Address Pattern matches the corresponding part of the OSC Address.
        /// </para>
        /// <para>
        /// A part of an OSC Address Pattern matches a part of an OSC Address if every consecutive character in the OSC Address
        /// Pattern matches the next consecutive substring of the OSC Address and every character in the OSC Address is matched
        /// by something in the OSC Address Pattern. These are the matching rules for characters in the OSC Address Pattern:
        /// </para>
        /// <para>
        /// 1. ‘?’ in the OSC Address Pattern matches any single character in the address part.
        /// </para>
        /// <para>
        /// 2. ‘*’ in the OSC Address Pattern matches any sequence of zero or more characters within the address part.
        /// </para>
        /// <para>
        /// 3. ‘//’ in the OSC Address Pattern matches any number of subsequent address parts.
        /// </para>
        /// <para>
        /// 4. A string of characters in square brackets (e.g., “[string]”) in the OSC Address Pattern matches any character in the string.
        /// Inside square brackets, the minus sign (-) and exclamation point (!) have special meanings. Two characters separated by a minus
        /// sign indicate the range of characters between the given two in ASCII collating sequence. A minus sign at the end of the string
        /// has no special meaning. An exclamation point at the beginning of a bracketed string negates the sense of the list, meaning that
        /// the list matches any character not in the list. An exclamation point anywhere besides the first character after the open bracket
        /// has no special meaning.
        /// </para>
        /// <para>
        /// 5. A comma-separated list of strings enclosed in curly braces (e.g., “{foo,bar}”) in the OSC Address Pattern matches any
        /// of the strings in the list.
        /// </para>
        /// <para>
        /// 6. Any other character in an OSC Address Pattern can match only the same character.
        /// </para>
        /// <para>
        /// If an OSC Address Pattern is matched against another OSC Address Pattern, they only match if they are identical.
        /// </para>
        /// <para>
        /// This implementation follows the OSC 1.1 specification.
        /// </para>
        /// </remarks>
        /// <param name="address">The address to check.</param>
        /// <returns><see langword="true"/> if the addresses represent the same address; otherwise, <see langword="false"/>.</returns>
        public bool Matches(in OscAddress address)
        {
            // don't match using invalid patterns
            if (Type == AddressType.Invalid || address.Type == AddressType.Invalid)
            {
                return false;
            }

            // Addresses only match identical addresses.
            // Patterns cannot be properly matched with each other. While it is theoretically possible to handle some cases,
            // there is too much complexity to be worthwhile. For now, only identical patterns will be matched.
            if (Type == address.Type)
            {
                return Equals(in address);
            }

            // match the pattern string against the address string
            return Type == AddressType.Pattern ? PatternMatching.Match(Pointer, address.Pointer) : PatternMatching.Match(address.Pointer, Pointer);
        }

        /// <summary>
        /// Coverts the address pattern into a string.
        /// </summary>
        /// <returns>A new string instance.</returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(Pointer, Length);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="OscAddress"/>.
        /// </summary>
        /// <param name="other">A value to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="other"/> has the same value as this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(in OscAddress other)
        {
            return Length == other.Length && UnsafeUtility.MemCmp(Pointer, other.Pointer, Length) == 0;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="OscAddress"/>.
        /// </summary>
        /// <param name="other">A value to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="other"/> has the same value as this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(OscAddress other)
        {
            return Length == other.Length && UnsafeUtility.MemCmp(Pointer, other.Pointer, Length) == 0;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="OscAddress"/> and equals
        /// the value of this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is OscAddress other && Equals(in other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Length * 397) ^ Pointer[Length - 1];
            }
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="OscAddress"/> are equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(in OscAddress a, in OscAddress b)
        {
            return a.Equals(in b);
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="OscAddress"/> are not equal.
        /// </summary>
        /// <param name="a">The first instance to compare.</param>
        /// <param name="b">The second instance to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> do not represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(in OscAddress a, in OscAddress b)
        {
            return !a.Equals(in b);
        }
    }
}
