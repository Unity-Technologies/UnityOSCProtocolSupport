using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(TimeTag))]
    sealed class ArgumentOutputTimeTag : ArgumentOutputEquatable<TimeTag>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.TimeTag };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteTimeTag(Value);
        }
    }
}
