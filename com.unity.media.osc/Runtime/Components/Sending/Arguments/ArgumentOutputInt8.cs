using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(sbyte))]
    sealed class ArgumentOutputInt8 : ArgumentOutputEquatable<sbyte>
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
