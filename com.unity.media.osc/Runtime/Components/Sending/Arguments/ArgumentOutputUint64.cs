using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(ulong))]
    sealed class ArgumentOutputUInt64 : ArgumentOutputEquatable<ulong>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Int64 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteInt64((long)Value);
        }
    }
}
