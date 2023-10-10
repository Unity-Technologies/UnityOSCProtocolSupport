using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class that represents an OSC Server.
    /// </summary>
    /// <remarks>
    /// OSC Servers are responsible for receiving and parsing messages, then executing the registered callbacks.
    /// This class is not thread-safe, but may be used on any thread.
    /// </remarks>
    public abstract class OscServer : IDisposable
    {
        /// <summary>
        /// An event invoked when any OSC Server receives an OSC Message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The event may be executed from the thread pool so you must only use thread-safe APIs.
        /// When there are any subscribers to this event extra processing is done to parse the messages,
        /// so it is recommended to only use this event for debugging purposes.
        /// </para>
        /// <para>
        /// * The first parameter is the server that received the message.<br/>
        /// * The second parameter is the parsed message instance. The message reference is only valid for the duration of the callback scope.<br/>
        /// * The third parameter is a string describing the message origin.<br/>
        /// </para>
        /// </remarks>
        public static event Action<OscServer, OscMessage, string> MessageReceived;

        /// <summary>
        /// Indicates whether there are any subscribers to the <see cref="MessageReceived"/> event.
        /// </summary>
        protected static bool IsMonitorCallbackRegistered => MessageReceived != null;

        internal static List<OscServer> Servers { get; } = new List<OscServer>();
        internal static event Action ServersChanged;

        [ThreadStatic]
        static List<OscCallbacks> s_TempCallbacks;

        static OscServer()
        {
            OscManager.PreUpdate += ServerUpdate;
        }

        static void ServerUpdate()
        {
            Profiler.BeginSample(nameof(ServerUpdate));

            try
            {
                foreach (var server in Servers)
                {
                    server.Update();
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        readonly ConcurrentQueue<Action> m_MainThreadQueue = new ConcurrentQueue<Action>();
        volatile bool m_Disposed;

        /// <summary>
        /// Has this instance been disposed.
        /// </summary>
        public bool IsDisposed => m_Disposed;

        /// <summary>
        /// The address space used to register callbacks to the server.
        /// </summary>
        public OscAddressSpace AddressSpace { get; } = new OscAddressSpace();

        /// <summary>
        /// Creates a new <see cref="OscServer"/> instance.
        /// </summary>
        protected OscServer()
        {
            Servers.Add(this);
            ServersChanged?.Invoke();

#if UNITY_EDITOR
            EditorApplication.quitting += Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#else
            Application.quitting += Dispose;
#endif
        }

        /// <summary>
        /// Disposes this instance in case it was not properly disposed.
        /// </summary>
        ~OscServer()
        {
            Debug.LogAssertion($"An instance of type \"{GetType().FullName}\" was not disposed!");
            Dispose(false);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            OnDispose(disposing);

            AddressSpace?.Dispose();

            if (Servers.Remove(this))
            {
                ServersChanged?.Invoke();
            }

#if UNITY_EDITOR
            EditorApplication.quitting -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
#else
            Application.quitting -= Dispose;
#endif

            m_Disposed = true;
        }

        /// <summary>
        /// Cleans up resources held by this instance.
        /// </summary>
        /// <param name="disposing">This is <see langword="true"/> when <see cref="Dispose"/> was called, and <see langword="false"/>
        /// when the instance is being disposed on the finalizer thread.</param>
        protected virtual void OnDispose(bool disposing)
        {
        }

        /// <summary>
        /// Gets the <see cref="Status"/> of the server.
        /// </summary>
        /// <param name="message">A detailed explanation of the current status.</param>
        /// <returns>The status of this server.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public virtual Status GetStatus(out string message)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscServer));

            message = string.Empty;
            return Status.None;
        }

        void Update()
        {
            OnUpdate();

            while (m_MainThreadQueue.TryDequeue(out var mainThreadAction))
            {
                try
                {
                    mainThreadAction.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Called on the main thread once per frame before Update.
        /// </summary>
        protected virtual void OnUpdate()
        {
        }

        /// <summary>
        /// Parses an OSC Packet and executes any matching callbacks for all of the messages in the packet.
        /// </summary>
        /// <remarks>
        /// This may be called from any thread.
        /// </remarks>
        /// <param name="packet">The packet buffer containing a single, complete OSC packet.</param>
        /// <param name="size">The length of the packet contents in bytes.</param>
        /// <param name="origin">A description of the source of the packet. Set this value to provide the message
        /// origin to any registered monitoring callbacks.</param>
        protected void HandlePacket(OscPacket packet, int size, string origin = null)
        {
            Profiler.BeginSample(nameof(HandlePacket));

            try
            {
                packet.Parse(size);

                var root = packet.RootElement;

                if (!root.IsValid)
                    return;

                switch (root)
                {
                    case OscMessage message:
                    {
                        HandleMessage(message, origin);
                        break;
                    }
                    case OscBundle bundle:
                    {
                        HandleBundle(bundle, origin);
                        break;
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        void HandleBundle(OscBundle bundle, string origin)
        {
            // Process messages first, messages from the same bundle must be
            // executed sequentially without other processing between.
            for (var i = 0; i < bundle.MessageCount; i++)
            {
                var message = bundle.GetMessage(i);

                if (message.IsValid)
                {
                    HandleMessage(message, origin);
                }
            }

            // Process any messages in nested bundles
            for (var i = 0; i < bundle.BundleCount; i++)
            {
                var nestedBundle = bundle.GetBundle(i);

                if (nestedBundle.IsValid)
                {
                    HandleBundle(nestedBundle, origin);
                }
            }
        }

        void HandleMessage(OscMessage message, string origin)
        {
            using var pattern = message.GetAddressPattern();

            if (s_TempCallbacks == null)
            {
                s_TempCallbacks = new List<OscCallbacks>();
            }

            AddressSpace.FindMatchingCallbacks(pattern, s_TempCallbacks);

            for (var i = 0; i < s_TempCallbacks.Count; i++)
            {
                var callbacks = s_TempCallbacks[i];

                try
                {
                    callbacks.ReadMessage?.Invoke(message);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (callbacks.MainThreadQueued != null)
                {
                    m_MainThreadQueue.Enqueue(callbacks.MainThreadQueued);
                }
            }

            if (IsMonitorCallbackRegistered)
            {
                MessageReceived.Invoke(this, message, origin);
            }
        }
    }
}
