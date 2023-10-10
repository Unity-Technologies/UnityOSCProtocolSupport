using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(int))]
    sealed class ArgumentOutputInt32 : ArgumentOutputEquatable<int>
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
