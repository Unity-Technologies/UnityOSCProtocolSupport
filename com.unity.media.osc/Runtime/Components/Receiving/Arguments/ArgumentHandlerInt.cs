using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Int")]
    sealed class ArgumentHandlerInt : ArgumentHandler<int>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out int value)
        {
            value = message.ReadInt32(argumentIndex);
            return true;
        }
    }
}
