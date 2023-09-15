using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a string argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The string value if the element has a matching tag; otherwise, an empty string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public string ReadString(int index)
        {
            CheckIndexValid(index);

            var offset = m_Offsets[index];

            return m_Tags[index] switch
            {
                TypeTag.String => ReadStringUnchecked(offset),
                TypeTag.AltTypeString => ReadStringUnchecked(offset),
                TypeTag.Float64 => ReadFloat64Unchecked(offset).ToString(),
                TypeTag.Float32 => ReadFloat32Unchecked(offset).ToString(),
                TypeTag.Int64 => ReadInt64Unchecked(offset).ToString(),
                TypeTag.Int32 => ReadInt32Unchecked(offset).ToString(),
                TypeTag.False => "False",
                TypeTag.True => "True",
                TypeTag.Nil => "Nil",
                TypeTag.Infinitum => "Infinitum",
                TypeTag.AsciiChar32 => ReadAsciiCharUnchecked(offset).ToString(),
                TypeTag.Color32 => ReadColor32Unchecked(offset).ToString(),
                TypeTag.MIDI => ReadMidiUnchecked(offset).ToString(),
                TypeTag.TimeTag => ReadTimeTagUnchecked(offset).ToString(),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Gets the string data buffer for a string argument.
        /// </summary>
        /// <remarks>
        /// The string is ASCII encoded, so each character is one byte. The string buffer always is followed by
        /// a null terminator.
        /// </remarks>
        /// <param name="index">The index of the argument to read.</param>
        /// <param name="stringPtr">A pointer to the start of the string. Returns <see langword="default"/> if the argument is not a string.</param>
        /// <param name="length">The length of the string in bytes, excluding the null terminator.</param>
        /// <returns><see langword="true"/> if the argument is a string; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public bool TryAccessString(int index, out IntPtr stringPtr, out int length)
        {
            CheckIndexValid(index);

            switch (m_Tags[index])
            {
                case TypeTag.String:
                case TypeTag.AltTypeString:
                {
                    var ptr = ElementPtr + m_Offsets[index];
                    stringPtr = (IntPtr)ptr;
                    return TryGetStringLength(ptr, out length);
                }
            }

            stringPtr = default;
            length = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string ReadStringUnchecked(int offset)
        {
            return Marshal.PtrToStringAnsi((IntPtr)(ElementPtr + offset));
        }
    }
}
