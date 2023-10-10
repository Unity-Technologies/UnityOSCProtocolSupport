using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Rect")]
    sealed class ArgumentHandlerRect : ArgumentHandler<Rect>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 4;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Rect value)
        {
            value = new Rect(
                message.ReadFloat32(argumentIndex),
                message.ReadFloat32(argumentIndex + 1),
                message.ReadFloat32(argumentIndex + 2),
                message.ReadFloat32(argumentIndex + 3)
            );
            return true;
        }
    }
}
