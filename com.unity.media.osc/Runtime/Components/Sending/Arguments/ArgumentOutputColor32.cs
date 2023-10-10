using System;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(Color32))]
    sealed class ArgumentOutputColor32 : ArgumentOutput<Color32>
    {
        Color32? m_Value;

        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.Color32 };

        /// <inheritdoc />
        protected override ArgumentDirtyFlags UpdateValue(Color32 value)
        {
            if (!m_Value.HasValue || !m_Value.Value.Equals(value))
            {
                m_Value = value;
                return ArgumentDirtyFlags.Value;
            }

            return ArgumentDirtyFlags.None;
        }

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteColor(m_Value ?? default);
        }
    }
}
