using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(double))]
    sealed class ArgumentOutputFloat64 : ArgumentOutputEquatable<double>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Float64 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteFloat64(Value);
        }
    }
}
