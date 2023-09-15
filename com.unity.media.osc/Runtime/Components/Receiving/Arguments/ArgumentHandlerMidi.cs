using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("MIDI")]
    sealed class ArgumentHandlerMidi : ArgumentHandler<MidiMessage>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out MidiMessage value)
        {
            value = message.ReadMidi(argumentIndex);
            return true;
        }
    }
}
