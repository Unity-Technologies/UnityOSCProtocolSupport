using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a 32-bit RGBA color argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The color value if the argument has a matching tag; otherwise, <see langword="default"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public Color32 ReadColor32(int index)
        {
            CheckIndexValid(index);

            return m_Tags[index] switch
            {
                TypeTag.Color32 => ReadColor32Unchecked(m_Offsets[index]),
                _ => default,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Color32 ReadColor32Unchecked(int offset)
        {
            return *(Color32*)(ElementPtr + offset);
        }
    }
}
