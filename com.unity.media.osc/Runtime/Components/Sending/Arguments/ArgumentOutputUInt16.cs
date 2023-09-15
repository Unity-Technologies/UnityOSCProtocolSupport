using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(ushort))]
    sealed class ArgumentOutputUInt16 : ArgumentOutputEquatable<ushort>
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
