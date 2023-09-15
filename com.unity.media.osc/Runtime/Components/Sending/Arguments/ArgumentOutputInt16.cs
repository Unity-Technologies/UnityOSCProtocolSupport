using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(short))]
    sealed class ArgumentOutputInt16 : ArgumentOutputEquatable<short>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Int32 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteInt32(Value);
        }
    }
}
