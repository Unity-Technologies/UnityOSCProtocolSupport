using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Time")]
    sealed class ArgumentHandlerTimeTag : ArgumentHandler<TimeTag>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out TimeTag value)
        {
            value = message.ReadTimeTag(argumentIndex);
            return true;
        }
    }
}
