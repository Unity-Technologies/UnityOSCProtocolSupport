using System;
using System.Net;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class which uses sockets to send OSC Messages.
    /// </summary>
    public abstract class OscIpClient : OscClient
    {
        IPEndPoint m_EndPoint;
        volatile bool m_IsRunning;
        readonly object m_Lock = new object();

        /// <summary>
        /// Is the client started.
        /// </summary>
        public bool IsRunning => m_IsRunning;

        /// <summary>
        /// The IP address and port to send messages to.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the value is set to null.</exception>
        public IPEndPoint EndPoint
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpClient));

                return m_EndPoint;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpClient));
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!m_EndPoint.Equals(value))
                {
                    m_EndPoint = value;
                    RestartIfRunning();
                }
            }
        }

        /// <inheritdoc />
        public override bool IsReady => base.IsReady && IsRunning;

        /// <inheritdoc />
        protected override string Destination => m_EndPoint.ToString();

        /// <summary>
        /// Creates a new <see cref="OscIpClient"/> instance.
        /// </summary>
        /// <param name="endPoint">The IP address and port to send messages to.</param>
        /// <param name="bufferSize">The size of the buffer used to write and send messages. Must be large enough
        /// to fit the OSC Messages to send, otherwise the messages cannot be sent.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endPoint"/> is null.</exception>
        protected OscIpClient(IPEndPoint endPoint, int bufferSize) : base(bufferSize)
        {
            m_EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        /// <inheritdoc />
        protected override void OnDispose(bool disposing)
        {
            Stop();
        }

        /// <summary>
        /// Begin sending OSC messages.
        /// </summary>
        /// <returns><see langword="true"/> if the client started successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public bool Start()
        {
            lock (m_Lock)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpClient));

                if (IsRunning)
                    return true;

                if (!NetworkingUtils.IsPortValid(m_EndPoint.Port, out var portMessage))
                {
                    Debug.LogError($"Unable to start {GetType().Name}: Port {m_EndPoint.Port} is not valid. {portMessage}");
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
        /// Stop sending OSC messages.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public void Stop()
        {
            lock (m_Lock)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscIpClient));

                m_IsRunning = false;

                OnStop();
            }
        }

        /// <inheritdoc />
        public override Status GetStatus(out string message)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OscIpClient));

            if (IsRunning)
            {
                message = "Running.";
                return Status.Ok;
            }
            if (!NetworkingUtils.IsPortValid(m_EndPoint.Port, out var portMessage))
            {
                message = $"Port {m_EndPoint.Port} is not valid. {portMessage}";
                return Status.Error;
            }

            message = "Not running.";
            return Status.None;
        }

        /// <summary>
        /// Restarts the client if it is running.
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
        /// Called when the client is starting.
        /// </summary>
        /// <returns><see langword="true"/> if the client started successfully; otherwise, <see langword="false"/>.</returns>
        protected abstract bool OnStart();

        /// <summary>
        /// Called when the client is stopping or failed to start.
        /// </summary>
        protected abstract void OnStop();
    }
}
