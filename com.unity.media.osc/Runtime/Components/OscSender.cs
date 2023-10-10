using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The base class to use for components that send OSC Messages.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class OscSender : MonoBehaviour
    {
#if UNITY_EDITOR
        readonly Dictionary<string, int> m_AddressRefCount = new Dictionary<string, int>();

        internal List<string> Addresses { get; } = new List<string>();
        internal event Action AddressesChanged;
#endif

        /// <summary>
        /// The client instance to use to send messages.
        /// </summary>
        public OscClient Client { get; private set; }

        /// <summary>
        /// This method is called by Unity when the component becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            OscManager.AutoBundlesUpdate += SendAutoBundle;
        }

        /// <summary>
        /// This method is called by Unity when the component becomes disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            OscManager.AutoBundlesUpdate -= SendAutoBundle;
        }

        void SendAutoBundle()
        {
            if (Client == null)
            {
                return;
            }

            Profiler.BeginSample(nameof(SendAutoBundle));

            try
            {
                Client.SendAutoBundle();
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// Sets the client instance to use to send messages.
        /// </summary>
        /// <param name="client">The client to use.</param>
        protected void SetClient(OscClient client)
        {
            if (Client != client)
            {
                OnClientChange(Client, client);
                Client = client;
            }
        }

        /// <summary>
        /// The sender calls this method when the client assigned to this sender has changed.
        /// </summary>
        /// <param name="oldClient">The previous client instance.</param>
        /// <param name="newClient">The new client instance.</param>
        protected virtual void OnClientChange(OscClient oldClient, OscClient newClient)
        {
        }

        /// <summary>
        /// Registers an address with this sender.
        /// </summary>
        /// <remarks>
        /// This allows the sender to track which addresses are associated with it.
        /// Registering an address is for informational purposes only and is not required when sending messages.
        /// Reference counting is used to allow separate registrants to use the same address, so each registrant should
        /// have exactly one matching call to <see cref="DeregisterAddress"/>.
        /// </remarks>
        /// <param name="address">The address to register.</param>
        [Conditional("UNITY_EDITOR")]
        public void RegisterAddress(string address)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            if (m_AddressRefCount.TryGetValue(address, out var refCount))
            {
                m_AddressRefCount[address] = refCount + 1;
                return;
            }

            m_AddressRefCount.Add(address, 1);
            Addresses.Add(address);
            Addresses.Sort();
            AddressesChanged?.Invoke();
#endif
        }

        /// <summary>
        /// Deregisters an address from this sender.
        /// </summary>
        /// <param name="address">The address to deregister.</param>
        [Conditional("UNITY_EDITOR")]
        public void DeregisterAddress(string address)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            if (!m_AddressRefCount.TryGetValue(address, out var refCount))
            {
                return;
            }

            refCount--;

            if (refCount <= 0)
            {
                m_AddressRefCount.Remove(address);
                Addresses.Remove(address);
                AddressesChanged?.Invoke();
            }
            else
            {
                m_AddressRefCount[address] = refCount;
            }
#endif
        }

        internal bool TryGetClient<T>(out T client) where T : OscClient
        {
            client = Client as T;
            return client != null;
        }
    }
}
