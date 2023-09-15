using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Vector3")]
    sealed class ArgumentHandlerVector3 : ArgumentHandler<Vector3>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 3;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Vector3 value)
        {
            value.x = message.ReadFloat32(argumentIndex);
            value.y = message.ReadFloat32(argumentIndex + 1);
            value.z = message.ReadFloat32(argumentIndex + 2);
            return true;
        }
    }
}
