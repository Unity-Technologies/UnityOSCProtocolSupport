using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(float))]
    sealed class ArgumentOutputFloat32 : ArgumentOutputEquatable<float>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Float32 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteFloat32(Value);
        }
    }
}
