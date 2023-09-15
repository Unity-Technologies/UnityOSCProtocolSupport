using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The base class to use for components that apply data from received OSC Messages to the Unity Scene.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class OscMessageHandlerBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The OSC Receiver to handle messages from.")]
        OscReceiver m_Receiver;
        [SerializeField, Tooltip("The OSC Address Pattern to associate with this message handler.")]
        string m_Address = "/";

        OscCallbacks m_Callbacks;
        OscReceiver m_RegisteredReceiver;
        string m_RegisteredAddress;

        /// <summary>
        /// The OSC Receiver to handle messages from.
        /// </summary>
        public OscReceiver Receiver
        {
            get => m_Receiver;
            set
            {
                if (m_Receiver != value)
                {
                    m_Receiver = value;
                    RegisterCallbacks();
                }
            }
        }

        /// <summary>
        /// The OSC Address Pattern to associate with this message handler.
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
                    RegisterCallbacks();
                }
            }
        }

        /// <summary>
        /// Resets the component to its default values.
        /// </summary>
        protected virtual void Reset()
        {
            Receiver = ComponentUtils.FindComponentInSameScene<OscReceiver>(gameObject);
            Address = $"/{name}";
        }

        /// <summary>
        /// Editor-only method called by Unity when the component is loaded or a value changes in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            OscUtils.ValidateAddress(ref m_Address, AddressType.Pattern);
            RegisterCallbacks();
        }

        /// <summary>
        /// This method is called by Unity when the component becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            RegisterCallbacks();
        }

        /// <summary>
        /// This method is called by Unity when the component becomes disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            DeregisterCallbacks();
        }

        void RegisterCallbacks()
        {
            if (m_Address == m_RegisteredAddress && m_Receiver == m_RegisteredReceiver)
            {
                return;
            }

            DeregisterCallbacks();

            if (m_Receiver != null && m_Address != null)
            {
                if (m_Callbacks == null)
                    m_Callbacks = new OscCallbacks(ValueRead, MainThreadAction);

                m_Receiver.AddCallback(m_Address, m_Callbacks);

                m_RegisteredReceiver = m_Receiver;
                m_RegisteredAddress = m_Address;
            }
        }

        void DeregisterCallbacks()
        {
            if (m_RegisteredReceiver != null && m_RegisteredAddress != null)
            {
                m_RegisteredReceiver.RemoveCallback(m_RegisteredAddress, m_Callbacks);
            }

            m_RegisteredReceiver = null;
            m_RegisteredAddress = null;
        }

        /// <summary>
        /// The message handler invoked to read the contents of an OSC Message.
        /// </summary>
        /// <remarks>
        /// This is not invoked from the main thread, so only use thread-safe APIs.
        /// </remarks>
        /// <param name="message">The message to read.</param>
        protected abstract void ValueRead(OscMessage message);

        /// <summary>
        /// A message handler invoked on the main thread after <see cref="ValueRead"/>.
        /// </summary>
        protected virtual void MainThreadAction()
        {
        }
    }
}
