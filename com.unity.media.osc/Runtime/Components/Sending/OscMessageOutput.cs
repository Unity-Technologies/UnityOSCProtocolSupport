using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// Use this component to send OSC Messages consisting of data from the Unity Scene via reflection.
    /// </summary>
    [AddComponentMenu("OSC/OSC Message Output")]
    public sealed class OscMessageOutput : OscMessageOutputBase
    {
        static readonly StringBuilder s_Builder = new StringBuilder();

        [SerializeField]
        List<PropertyOutput> m_Arguments = new List<PropertyOutput>();

        string m_TagString;
        ArgumentDirtyFlags m_DirtyFlags;

        /// <inheritdoc />
        protected override void OnValidate()
        {
            base.OnValidate();

            m_DirtyFlags = ArgumentDirtyFlags.All;
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_DirtyFlags = ArgumentDirtyFlags.All;
        }

        /// <inheritdoc />
        protected override void OnUpdate(OscClient client, OscAddress address)
        {
            if (m_Arguments.Count == 0)
            {
                return;
            }

            foreach (var property in m_Arguments)
            {
                m_DirtyFlags |= property.Update();
            }

            // when the composition of the message has changed we must rebuild the tag string
            if ((m_DirtyFlags & ArgumentDirtyFlags.Tags) != 0)
            {
                s_Builder.Clear();
                s_Builder.Append(',');

                foreach (var property in m_Arguments)
                {
                    if (property.TryGetTags(out var tags))
                    {
                        for (var i = 0; i < tags.Length; i++)
                        {
                            s_Builder.Append((char)(byte)tags[i]);
                        }
                    }
                }

                m_TagString = s_Builder.ToString();
            }

            // only send messages when the value has changed since the last sent message
            if (m_DirtyFlags != ArgumentDirtyFlags.None)
            {
                using (client.BeginMessage(address, m_TagString))
                {
                    foreach (var property in m_Arguments)
                    {
                        property.Write(client);
                    }
                }
            }

            m_DirtyFlags = ArgumentDirtyFlags.None;
        }

        internal void AddArgumentOutput()
        {
            m_Arguments.Add(new PropertyOutput(gameObject));
        }

        internal MemberInfo[] GetArgumentMembers()
        {
            return m_Arguments
                .Select(a => a.GetArgumentMember())
                .Where(m => m != null)
                .ToArray();
        }
    }
}
