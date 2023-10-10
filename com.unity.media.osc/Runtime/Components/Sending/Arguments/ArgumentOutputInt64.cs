using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(long))]
    sealed class ArgumentOutputInt64 : ArgumentOutputEquatable<long>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Int64 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteInt64(Value);
        }
    }
}
