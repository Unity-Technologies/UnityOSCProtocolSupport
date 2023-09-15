using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Vector2")]
    sealed class ArgumentHandlerVector2 : ArgumentHandler<Vector2>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 2;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Vector2 value)
        {
            value.x = message.ReadFloat32(argumentIndex);
            value.y = message.ReadFloat32(argumentIndex + 1);
            return true;
        }
    }
}
