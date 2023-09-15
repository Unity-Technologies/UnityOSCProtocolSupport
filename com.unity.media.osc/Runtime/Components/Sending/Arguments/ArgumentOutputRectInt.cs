using System;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(RectInt))]
    sealed class ArgumentOutputRectInt : ArgumentOutputEquatable<RectInt>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } =
        {
            TypeTag.Int32,
            TypeTag.Int32,
            TypeTag.Int32,
            TypeTag.Int32,
        };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            var value = Value;

            sender.WriteInt32(value.x);
            sender.WriteInt32(value.y);
            sender.WriteInt32(value.width);
            sender.WriteInt32(value.height);
        }
    }
}
