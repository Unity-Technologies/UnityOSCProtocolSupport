using System;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(Matrix4x4))]
    sealed class ArgumentOutputMatrix4x4 : ArgumentOutputEquatable<Matrix4x4>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } =
        {
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,

            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,

            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,

            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
        };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            var value = Value;

            sender.WriteFloat32(value.m00);
            sender.WriteFloat32(value.m01);
            sender.WriteFloat32(value.m02);
            sender.WriteFloat32(value.m03);

            sender.WriteFloat32(value.m10);
            sender.WriteFloat32(value.m11);
            sender.WriteFloat32(value.m12);
            sender.WriteFloat32(value.m13);

            sender.WriteFloat32(value.m20);
            sender.WriteFloat32(value.m21);
            sender.WriteFloat32(value.m22);
            sender.WriteFloat32(value.m23);

            sender.WriteFloat32(value.m30);
            sender.WriteFloat32(value.m31);
            sender.WriteFloat32(value.m32);
            sender.WriteFloat32(value.m33);
        }
    }
}
