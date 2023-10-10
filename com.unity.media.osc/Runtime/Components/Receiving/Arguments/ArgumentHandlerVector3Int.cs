using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Vector3Int")]
    sealed class ArgumentHandlerVector3Int : ArgumentHandler<Vector3Int>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 3;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Vector3Int value)
        {
            value = new Vector3Int(
                message.ReadInt32(argumentIndex),
                message.ReadInt32(argumentIndex + 1),
                message.ReadInt32(argumentIndex + 2)
            );
            return true;
        }
    }
}
