using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    [Serializable]
    [ArgumentHandler("Matrix4x4")]
    sealed class ArgumentHandlerMatrix4x4 : ArgumentHandler<Matrix4x4>
    {
        /// <inheritdoc />
        protected override int ArgumentCount => 16;

        /// <inheritdoc />
        protected override bool TryReadArgument(OscMessage message, int argumentIndex, out Matrix4x4 value)
        {
            value.m00 = message.ReadFloat32(argumentIndex);
            value.m01 = message.ReadFloat32(argumentIndex + 1);
            value.m02 = message.ReadFloat32(argumentIndex + 2);
            value.m03 = message.ReadFloat32(argumentIndex + 3);
            value.m10 = message.ReadFloat32(argumentIndex + 4);
            value.m11 = message.ReadFloat32(argumentIndex + 5);
            value.m12 = message.ReadFloat32(argumentIndex + 6);
            value.m13 = message.ReadFloat32(argumentIndex + 7);
            value.m20 = message.ReadFloat32(argumentIndex + 8);
            value.m21 = message.ReadFloat32(argumentIndex + 9);
            value.m22 = message.ReadFloat32(argumentIndex + 10);
            value.m23 = message.ReadFloat32(argumentIndex + 11);
            value.m30 = message.ReadFloat32(argumentIndex + 12);
            value.m31 = message.ReadFloat32(argumentIndex + 13);
            value.m32 = message.ReadFloat32(argumentIndex + 14);
            value.m33 = message.ReadFloat32(argumentIndex + 15);
            return true;
        }
    }
}
