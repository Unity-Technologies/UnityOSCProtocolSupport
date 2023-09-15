using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Double")]
    sealed class ArgumentHandlerDouble : ArgumentHandler<double>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out double value)
        {
            value = message.ReadFloat64(argumentIndex);
            return true;
        }
    }
}
