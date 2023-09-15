using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Quaternion")]
    sealed class ArgumentHandlerQuaternion : ArgumentHandler<Quaternion>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 4;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Quaternion value)
        {
            value.x = message.ReadFloat32(argumentIndex);
            value.y = message.ReadFloat32(argumentIndex + 1);
            value.z = message.ReadFloat32(argumentIndex + 2);
            value.w = message.ReadFloat32(argumentIndex + 3);
            return true;
        }
    }
}
