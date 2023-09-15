using System;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(Color))]
    sealed class ArgumentOutputColor : ArgumentOutputEquatable<Color>
    {
        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Color32 };

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteColor(Value);
        }
    }
}
