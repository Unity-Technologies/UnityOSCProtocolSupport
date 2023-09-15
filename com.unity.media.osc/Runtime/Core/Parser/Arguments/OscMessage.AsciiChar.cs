using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read an ASCII char argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The character value if the element has a matching tag; otherwise, <see langword="default"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public char ReadAsciiChar(int index)
        {
            CheckIndexValid(index);

            return m_Tags[index] switch
            {
                TypeTag.AsciiChar32 => ReadAsciiCharUnchecked(m_Offsets[index]),
                _ => default,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        char ReadAsciiCharUnchecked(int offset)
        {
            return (char)ElementPtr[offset + 3];
        }
    }
}
