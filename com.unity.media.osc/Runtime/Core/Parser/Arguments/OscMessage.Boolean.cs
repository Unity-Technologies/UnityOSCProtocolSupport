using System;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a boolean argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The boolean value if the argument has a matching tag; otherwise, <see langword="default"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public bool ReadBoolean(int index)
        {
            CheckIndexValid(index);

            return m_Tags[index] switch
            {
                TypeTag.True => true,
                TypeTag.False => false,
                TypeTag.Int32 => ReadInt32Unchecked(m_Offsets[index]) != 0,
                _ => default,
            };
        }
    }
}
