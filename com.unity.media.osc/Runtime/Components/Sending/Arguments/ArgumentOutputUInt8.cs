using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(byte))]
    sealed class ArgumentOutputUInt8 : ArgumentOutputEquatable<byte>
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
