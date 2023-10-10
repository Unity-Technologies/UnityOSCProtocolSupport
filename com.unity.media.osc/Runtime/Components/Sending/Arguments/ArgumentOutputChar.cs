using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(char))]
    sealed class ArgumentOutputChar : ArgumentOutputEquatable<char>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.AsciiChar32 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteChar(Value);
        }
    }
}
