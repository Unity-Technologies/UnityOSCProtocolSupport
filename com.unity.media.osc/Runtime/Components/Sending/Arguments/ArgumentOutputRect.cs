using System;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(Rect))]
    sealed class ArgumentOutputRect : ArgumentOutputEquatable<Rect>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } =
        {
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
            TypeTag.Float32,
        };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            var value = Value;

            sender.WriteFloat32(value.x);
            sender.WriteFloat32(value.y);
            sender.WriteFloat32(value.width);
            sender.WriteFloat32(value.height);
        }
    }
}
