using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Character")]
    sealed class ArgumentHandlerChar : ArgumentHandler<char>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out char value)
        {
            value = message.ReadAsciiChar(argumentIndex);
            return true;
        }
    }
}
