using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Long")]
    sealed class ArgumentHandlerLong : ArgumentHandler<long>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out long value)
        {
            value = message.ReadInt64(argumentIndex);
            return true;
        }
    }
}
