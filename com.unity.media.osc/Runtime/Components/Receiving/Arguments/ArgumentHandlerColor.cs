using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Color")]
    sealed class ArgumentHandlerColor : ArgumentHandler<Color>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 1;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Color value)
        {
            value = message.ReadColor32(argumentIndex);
            return true;
        }
    }
}
