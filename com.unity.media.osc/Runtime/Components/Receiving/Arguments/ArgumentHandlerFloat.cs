using System;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Float")]
    sealed class ArgumentHandlerFloat : ArgumentHandler<float>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out float value)
        {
            value = message.ReadFloat32(argumentIndex);
            return true;
        }
    }
}
