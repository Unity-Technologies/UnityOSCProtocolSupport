using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Vector2Int")]
    sealed class ArgumentHandlerVector2Int : ArgumentHandler<Vector2Int>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 2;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Vector2Int value)
        {
            value = new Vector2Int(
                message.ReadInt32(argumentIndex),
                message.ReadInt32(argumentIndex + 1)
            );
            return true;
        }
    }
}
