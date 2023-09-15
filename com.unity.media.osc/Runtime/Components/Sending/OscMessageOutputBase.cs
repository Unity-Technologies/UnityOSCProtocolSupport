using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The base class to use for components that send OSC Messages containing data from the scene.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class OscMessageOutputBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The OSC Sender to send messages with.")]
        OscSender m_Sender;
        [SerializeField, Tooltip("The OSC Address Pattern of the output messages.")]
        string m_Address = "/";

        bool m_ParsedAddressDirty;
        OscAddress? m_ParsedAddress;
        OscSender m_RegisteredSender;
        string m_RegisteredAddress;

        /// <summary>
        /// The OSC Sender to send messages with.
        /// </summary>
        public OscSender Sender
        {
            get => m_Sender;
            set
            {
                if (m_Sender != value)
                {
                    m_Sender = value;
                    RegisterAddress();
                }
            }
        }

        /// <summary>
        /// The OSC Address Pattern of the output messages.
        /// </summary>
        public string Address
        {
            get => m_Address;
            set
            {
                if (m_Address != value)
                {
                    m_Address = value;
                    OscUtils.ValidateAddress(ref m_Address, AddressType.Pattern);
                    m_ParsedAddressDirty = true;
                    RegisterAddress();
                }
            }
        }

        /// <summary>
        /// Resets the component to its default values.
        /// </summary>
        protected virtual void Reset()
        {
            Sender = ComponentUtils.FindComponentInSameScene<OscSender>(gameObject);
            Address = $"/{name}";
        }

        /// <summary>
        /// Editor-only method called by Unity when the component is loaded or a value changes in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            OscUtils.ValidateAddress(ref m_Address, AddressType.Pattern);
            m_ParsedAddressDirty = true;
            RegisterAddress();
        }

        /// <summary>
        /// This method is called by Unity when the component becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_ParsedAddressDirty = true;
            RegisterAddress();

            OscManager.MessageOutputUpdate += SendMessage;
        }

        /// <summary>
        /// This method is called by Unity when the component becomes disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            DisposeParsedAddress();
            DeregisterAddress();

            OscManager.MessageOutputUpdate -= SendMessage;
        }

        /// <summary>
        /// <see cref="OscMessageHandlerBase"/> calls this method every frame to send a message if needed.
        /// </summary>
        /// <param name="client">The client to send messages with.</param>
        /// <param name="address">The address to use for the messages.</param>
        protected abstract void OnUpdate(OscClient client, OscAddress address);

        void SendMessage()
        {
            if (m_Sender == null)
                return;

            var client = m_Sender.Client;

            if (client == null || client.IsDisposed || !client.IsReady)
                return;

            if (m_ParsedAddressDirty)
            {
                UpdateParsedAddress();
            }

            if (!m_ParsedAddress.HasValue)
                return;

            var address = m_ParsedAddress.Value;

            if (address.Type == AddressType.Invalid)
                return;

            OnUpdate(client, address);
        }

        void RegisterAddress()
        {
            if (m_Address == m_RegisteredAddress && m_Sender == m_RegisteredSender)
            {
                return;
            }

            DeregisterAddress();

            if (m_Sender != null && m_Address != null)
            {
                m_Sender.RegisterAddress(m_Address);

                m_RegisteredSender = m_Sender;
                m_RegisteredAddress = m_Address;
            }
        }

        void DeregisterAddress()
        {
            if (m_RegisteredSender != null && m_RegisteredAddress != null)
            {
                m_RegisteredSender.DeregisterAddress(m_RegisteredAddress);
            }

            m_RegisteredSender = null;
            m_RegisteredAddress = null;
        }

        void UpdateParsedAddress()
        {
            DisposeParsedAddress();

            m_ParsedAddress = m_Address != null ? new OscAddress(m_Address) : default;
            m_ParsedAddressDirty = false;
        }

        void DisposeParsedAddress()
        {
            if (m_ParsedAddress.HasValue)
            {
                m_ParsedAddress.Value.Dispose();
                m_ParsedAddress = null;
            }
        }
    }
}
