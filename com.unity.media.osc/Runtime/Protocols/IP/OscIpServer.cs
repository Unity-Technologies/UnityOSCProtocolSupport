using System;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class which uses sockets to receive OSC Messages.
    /// </summary>
    public abstract class OscIpServer : OscServer
    {
        int m_Port;
        volatile bool m_IsRunning;
        readonly object m_Lock = new object();

        /// <summary>
        /// Is the server started.
        /// </summary>
        public bool IsRunning => m_IsRunning;

        /// <summary>
        /// The networking port to listen for messages on.
        /// </summary>
        /// <remarks>
        /// It is supported for multiple servers to use the same port.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public int Port
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpServer));

                return m_Port;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpServer));

                if (m_Port != value)
                {
                    m_Port = value;
                    RestartIfRunning();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="OscIpServer"/> instance.
        /// </summary>
        /// <param name="port">The networking port to listen for messages on.</param>
        protected OscIpServer(int port)
        {
            m_Port = port;
        }

        /// <inheritdoc />
        protected override void OnDispose(bool disposing)
        {
            Stop();
        }

        /// <summary>
        /// Begin receiving OSC messages.
        /// </summary>
        /// <returns><see langword="true"/> if the server started successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public bool Start()
        {
            lock (m_Lock)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpServer));

                if (IsRunning)
                    return true;

                if (!NetworkingUtils.IsPortValid(m_Port, out var portMessage))
                {
                    Debug.LogError($"Unable to start {GetType().Name}: Port {m_Port} is not valid. {portMessage}");
                    return false;
                }

                bool success;

                try
                {
                    success = OnStart();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    success = false;
                }

                if (!success)
                {
                    OnStop();
                    return false;
                }

                m_IsRunning = true;
                return true;
            }
        }

        /// <summary>
        /// Stop receiving OSC messages.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public void Stop()
        {
            lock (m_Lock)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpServer));

                m_IsRunning = false;

                OnStop();
            }
        }

        /// <inheritdoc />
        public override Status GetStatus(out string message)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscIpServer));

            if (IsRunning)
            {
                message = $"Running on port {m_Port}.";
                return Status.Ok;
            }
            if (!NetworkingUtils.IsPortValid(m_Port, out var portMessage))
            {
                message = $"Port {m_Port} is not valid. {portMessage}";
                return Status.Error;
            }

            message = "Not running.";
            return Status.None;
        }

        /// <summary>
        /// Restarts the server if it is running.
        /// </summary>
        internal void RestartIfRunning()
        {
            if (IsRunning)
            {
                Stop();
                Start();
            }
        }

        /// <summary>
        /// Called when the server is starting.
        /// </summary>
        /// <returns><see langword="true"/> if the server started successfully; otherwise, <see langword="false"/>.</returns>
        protected abstract bool OnStart();

        /// <summary>
        /// Called when the server is stopping or failed to start.
        /// </summary>
        protected abstract void OnStop();
    }
}
