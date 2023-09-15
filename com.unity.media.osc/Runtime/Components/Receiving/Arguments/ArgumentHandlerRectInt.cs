using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("RectInt")]
    sealed class ArgumentHandlerRectInt : ArgumentHandler<RectInt>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 4;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out RectInt value)
        {
            value = new RectInt(
                message.ReadInt32(argumentIndex),
                message.ReadInt32(argumentIndex + 1),
                message.ReadInt32(argumentIndex + 2),
                message.ReadInt32(argumentIndex + 3)
            );
            return true;
        }
    }
}
