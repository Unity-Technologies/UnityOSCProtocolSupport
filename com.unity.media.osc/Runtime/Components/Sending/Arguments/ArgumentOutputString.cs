using System;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(string))]
    sealed class ArgumentOutputString : ArgumentOutput<string>
    {
        string m_Value;
        bool m_HasValue;

        /// <inheritdoc />
        public override TypeTag[] Tags { get; } = { TypeTag.String };

        /// <inheritdoc />
        protected override ArgumentDirtyFlags UpdateValue(string value)
        {
            if (!m_HasValue || m_Value != value)
            {
                m_Value = value;
                m_HasValue = true;
                return ArgumentDirtyFlags.Value;
            }

            return ArgumentDirtyFlags.None;
        }

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            sender.WriteString(m_Value ?? string.Empty);
        }
    }
}
