using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class <see cref="OscMessageOutput"/> uses to add arguments to OSC Messages.
    /// </summary>
    [Serializable]
    class PropertyOutput : ISerializationCallbackReceiver
    {
        internal enum MemberType
        {
            Invalid = 0,
            Field = 1,
            Property = 2,
            Method = 3,
        }

#pragma warning disable CS0414
        [SerializeField]
        GameObject m_Object = null;
        [SerializeField]
        Object m_Component = null;
        [SerializeField]
        MemberType m_MemberType = MemberType.Invalid;
        [SerializeField]
        string m_MemberValueType = null;
        [SerializeField]
        string m_MemberName = null;
        [SerializeReference]
        IArgumentOutput m_Output = null;
#pragma warning restore CS0414

        bool m_SourceDirty;
        ArgumentDirtyFlags m_DirtyFlags;

        internal PropertyOutput(GameObject obj)
        {
            m_Object = obj;
        }

        internal ArgumentDirtyFlags Update()
        {
            if (m_Output != null)
            {
                if (m_SourceDirty)
                {
                    m_Output.SetSource(m_Component, GetArgumentMember());
                    m_DirtyFlags |= ArgumentDirtyFlags.All;
                    m_SourceDirty = false;
                }

                if (m_Output.IsValid)
                {
                    m_DirtyFlags |= m_Output.UpdateValue();
                    return m_DirtyFlags;
                }
            }

            return ArgumentDirtyFlags.None;
        }

        internal bool TryGetTags(out TypeTag[] tags)
        {
            if (m_Output != null && m_Output.IsValid)
            {
                tags = m_Output.Tags;
                return true;
            }

            tags = default;
            return false;
        }

        internal void Write(OscClient sender)
        {
            if (m_Output != null && m_Output.IsValid)
            {
                m_Output.Write(sender);
                m_DirtyFlags = ArgumentDirtyFlags.None;
            }
        }

        internal MemberInfo GetArgumentMember()
        {
            if (m_Component == null)
            {
                return null;
            }

            var type = m_Component.GetType();

            try
            {
                return m_MemberType switch
                {
                    MemberType.Field => type.GetField(m_MemberName),
                    MemberType.Property => type.GetProperty(m_MemberName),
                    MemberType.Method => type.GetMethod(m_MemberName),
                    _ => null,
                };
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // We can't find the member on deserialization since it requires access to a UnityEngine.Object
            // which may not be used in this callback, so we defer initialization.
            m_SourceDirty = true;
        }
    }
}
