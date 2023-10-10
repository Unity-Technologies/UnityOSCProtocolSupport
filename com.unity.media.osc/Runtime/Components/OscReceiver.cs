using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The base class to use for components that receive OSC Messages.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class OscReceiver : MonoBehaviour
    {
        readonly Dictionary<string, HashSet<OscCallbacks>> m_Callbacks = new Dictionary<string, HashSet<OscCallbacks>>();

#if UNITY_EDITOR
        internal List<string> Addresses { get; } = new List<string>();
        internal event Action AddressesChanged;
#endif

        /// <summary>
        /// The server instance to use to receive messages.
        /// </summary>
        protected OscServer Server { get; private set; }

        /// <summary>
        /// Sets the server instance to use to receive messages.
        /// </summary>
        /// <param name="server">The server to use.</param>
        protected void SetServer(OscServer server)
        {
            if (Server == server)
                return;

            // remove callbacks from the old server
            if (Server != null)
            {
                foreach (var addressCallbacks in m_Callbacks)
                {
                    var address = addressCallbacks.Key;
                    var callbacks = addressCallbacks.Value;

                    foreach (var callback in callbacks)
                    {
                        Server.AddressSpace.RemoveCallback(address, callback);
                    }
                }
            }

            OnServerChange(Server, server);
            Server = server;

            // add callbacks to the new server
            if (Server != null)
            {
                foreach (var addressCallbacks in m_Callbacks)
                {
                    var address = addressCallbacks.Key;
                    var callbacks = addressCallbacks.Value;

                    foreach (var callback in callbacks)
                    {
                        Server.AddressSpace.TryAddCallback(address, callback);
                    }
                }
            }
        }

        /// <summary>
        /// The receiver calls this method when the server assigned to this receiver has changed.
        /// </summary>
        /// <param name="oldServer">The previous server instance.</param>
        /// <param name="newServer">The new server instance.</param>
        protected virtual void OnServerChange(OscServer oldServer, OscServer newServer)
        {
        }

        /// <summary>
        /// Adds callbacks to the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to add the callbacks to.</param>
        /// <param name="callbacks">The callbacks to add for the address.</param>
        /// <returns><see langword="true"/> if the callbacks were successfully added; otherwise, <see langword="false"/>.</returns>
        public bool AddCallback(string address, OscCallbacks callbacks)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (!m_Callbacks.TryGetValue(address, out var allCallbacks))
            {
                allCallbacks = new HashSet<OscCallbacks>();
                m_Callbacks.Add(address, allCallbacks);
#if UNITY_EDITOR
                Addresses.Add(address);
                Addresses.Sort();
                AddressesChanged?.Invoke();
#endif
            }

            if (!allCallbacks.Add(callbacks))
            {
                return false;
            }

            if (Server != null)
            {
                Server.AddressSpace.TryAddCallback(address, callbacks);
            }

            return true;
        }

        /// <summary>
        /// Removes callbacks from the specified address.
        /// </summary>
        /// <param name="address">The address or address pattern to remove the callbacks from.</param>
        /// <param name="callbacks">The callbacks to remove from the address.</param>
        /// <returns><see langword="true"/> if the callbacks were successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveCallback(string address, OscCallbacks callbacks)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (!m_Callbacks.TryGetValue(address, out var allCallbacks) || !allCallbacks.Remove(callbacks))
            {
                return false;
            }

            if (allCallbacks.Count == 0)
            {
                m_Callbacks.Remove(address);
#if UNITY_EDITOR
                Addresses.Remove(address);
                AddressesChanged?.Invoke();
#endif
            }

            if (Server != null)
            {
                Server.AddressSpace.RemoveCallback(address, callbacks);
            }

            return true;
        }

        internal bool TryGetServer<T>(out T server) where T : OscServer
        {
            server = Server as T;
            return server != null;
        }
    }
}
