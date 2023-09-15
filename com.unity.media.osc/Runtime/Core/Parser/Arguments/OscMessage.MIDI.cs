using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Media.Osc
{
    unsafe partial class OscMessage
    {
        /// <summary>
        /// Read a MIDI message argument.
        /// </summary>
        /// <param name="index">The index of the argument to read.</param>
        /// <returns>The MIDI message if the argument has a matching tag; otherwise, <see langword="default"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or exceeds the
        /// number of message arguments.</exception>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public MidiMessage ReadMidi(int index)
        {
            CheckIndexValid(index);

            return m_Tags[index] switch
            {
                TypeTag.MIDI => ReadMidiUnchecked(m_Offsets[index]),
                _ => default,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        MidiMessage ReadMidiUnchecked(int offset)
        {
            return *(MidiMessage*)(ElementPtr + offset);
        }
    }
}
