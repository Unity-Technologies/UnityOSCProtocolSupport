using System;
using UnityEngine;

namespace Unity.Media.Osc.Arguments
{
    [Serializable]
    [ArgumentOutput(typeof(bool))]
    sealed class ArgumentOutputBool : ArgumentOutput<bool>
    {
        [SerializeField, Tooltip("By default boolean values as sent as an integer. " +
             "Enable this option to send the value in the tag string using the \"T\" and \"F\" type tags.")]
        bool m_SendAsTags = false;

        readonly TypeTag[] s_Tags = new TypeTag[1];
        bool? m_Value;

        /// <inheritdoc />
        public override TypeTag[] Tags
        {
            get
            {
                if (m_SendAsTags)
                {
                    s_Tags[0] = (m_Value ?? false) ? TypeTag.True : TypeTag.False;
                }
                else
                {
                    s_Tags[0] = TypeTag.Int32;
                }
                return s_Tags;
            }
        }

        /// <inheritdoc />
        protected override ArgumentDirtyFlags UpdateValue(bool value)
        {
            if (!m_Value.HasValue || m_Value.Value != value)
            {
                m_Value = value;
                return m_SendAsTags ? ArgumentDirtyFlags.Tags : ArgumentDirtyFlags.Value;
            }

            return ArgumentDirtyFlags.None;
        }

        /// <inheritdoc />
        public override void Write(OscClient sender)
        {
            if (!m_SendAsTags)
            {
                sender.WriteInt32((m_Value ?? default) ? 1 : 0);
            }
        }
    }
}
