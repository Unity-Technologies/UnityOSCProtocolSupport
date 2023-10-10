using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Boolean")]
    sealed class ArgumentHandlerBool : ArgumentHandler<bool>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out bool value)
        {
            value = message.GetTag(argumentIndex) switch
            {
                TypeTag.False => false,
                TypeTag.True => true,
                _ => message.ReadInt32(argumentIndex) != 0,
            };
            return true;
        }
    }
}
