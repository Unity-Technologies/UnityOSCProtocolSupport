using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(MidiMessage))]
    sealed class ArgumentOutputMidi : ArgumentOutputEquatable<MidiMessage>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.MIDI };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteMidi(Value);
        }
    }
}
