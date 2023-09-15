using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("String")]
    sealed class ArgumentHandlerString : ArgumentHandler<string>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out string value)
        {
            value = message.ReadString(argumentIndex);
            return true;
        }
    }
}
