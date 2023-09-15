using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a time tag argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The time value if the argument has a matching tag; otherwise, <see langword="default"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public TimeTag ReadTimeTag(int index)
        {
            CheckIndexValid(index);

            return m_Tags[index] switch
            {
                TypeTag.TimeTag => ReadTimeTagUnchecked(m_Offsets[index]),
                _ => default,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        TimeTag ReadTimeTagUnchecked(int offset)
        {
            return TimeTag.FromBigEndianBytes(ElementPtr + offset);
        }
    }
}
