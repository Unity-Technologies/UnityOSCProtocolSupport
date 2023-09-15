using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a 32-bit float argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The float value if the argument has a matching tag; otherwise, <see langword="default"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public float ReadFloat32(int index)
        {
            CheckIndexValid(index);

            var offset = m_Offsets[index];

            return m_Tags[index] switch
            {
                TypeTag.Int32 => ReadInt32Unchecked(offset),
                TypeTag.Int64 => ReadInt64Unchecked(offset),
                TypeTag.Float32 => ReadFloat32Unchecked(offset),
                TypeTag.Float64 => (float)ReadFloat64Unchecked(offset),
                _ => default,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float ReadFloat32Unchecked(int offset)
        {
            var val = ReadInt32Unchecked(offset);
            return *(float*)&val;
        }
    }
}
