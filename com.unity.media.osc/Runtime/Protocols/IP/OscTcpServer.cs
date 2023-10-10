using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class which uses TCP sockets to receive OSC Messages.
    /// </summary>
    public sealed class OscTcpServer : OscIpServer
    {
        /// <summary>
        /// The size of the tcp backlog, which limits the number of connections which can be in queue
        /// to complete their handshake. Incoming connections will not succeed if the queue is full.
        /// </summary>
        const int k_MaxPendingConnections = 20;

        /// <summary>
        /// The time in milliseconds after which a connection should be closed if the client cannot be reached.
        /// </summary>
        const int k_Timeout = 10 * 1000;

        /// <summary>
        /// The time between attempts to create the listener socket if it fails to start.
        /// </summary>
        static readonly TimeSpan k_RetrySocketPeriod = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// The time between polling connected sockets to check which connections are still active.
        /// </summary>
        static readonly TimeSpan k_PollPeriod = TimeSpan.FromSeconds(1.0);

        static UpdateThread<OscTcpServer> s_UpdateThread = new("OSC", nameof(OscTcpServer), server =>
        {
            server.ReadPackets();
            server.UpdateConnections();
        });

        static OscTcpServer()
        {
            OscManager.NetworkUpdate += () => s_UpdateThread.QueueUpdate();
        }

        OscStreamType m_StreamType = OscStreamType.Slip;
        readonly List<OscStreamReader> m_Streams = new List<OscStreamReader>();
        readonly object m_StreamsLock = new object();
        CancellationTokenSource m_ListeningCancellationSource;
        bool m_Listening;
        Exception m_ListenerException;
        DateTime? m_LastPollTime;

        /// <summary>
        /// The framing used in the packet stream.
        /// </summary>
        /// <remarks>
        /// The stream type must match the configuration of the sending application in order for the stream to be read correctly.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public OscStreamType StreamType
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscTcpServer));

                return m_StreamType;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscTcpServer));

                if (m_StreamType != value)
                {
                    m_StreamType = value;
                    RestartIfRunning();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="OscTcpServer"/> instance.
        /// </summary>
        /// <param name="port">The networking port to listen for messages on.</param>
        public OscTcpServer(int port = OscConstants.DefaultPort) : base(port)
        {
        }

        /// <summary>
        /// Gets a string describing the server state.
        /// </summary>
        /// <returns>A string representing the server state.</returns>
        public override string ToString()
        {
            return $"TCP ({Port}, {m_StreamType.GetDisplayName()})";
        }

        /// <inheritdoc />
        public override Status GetStatus(out string message)
        {
            var status = base.GetStatus(out message);

            if (status == Status.Ok)
            {
                if (m_Listening)
                {
                    lock (m_StreamsLock)
                    {
                        message = $"{m_Streams.Count} connection{(m_Streams.Count != 1 ? "s" : string.Empty)} on port {Port}.";
                        status = m_Streams.Count <= 0 ? Status.Warning : Status.Ok;
                    }
                }
                else if (m_ListenerException != null)
                {
                    message = m_ListenerException.Message;
                    status = Status.Error;
                }
            }

            return status;
        }

        /// <inheritdoc />
        protected override bool OnStart()
        {
            m_LastPollTime = null;
            m_ListeningCancellationSource = new CancellationTokenSource();

            _ = ListenAsync(Port, m_ListeningCancellationSource.Token);

            s_UpdateThread.Add(this);
            return true;
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            // stop listening for new connections
            if (m_ListeningCancellationSource != null)
            {
                m_ListeningCancellationSource.Cancel();
                m_ListeningCancellationSource = null;
            }

            // close existing connections
            lock (m_StreamsLock)
            {
                foreach (var stream in m_Streams)
                {
                    stream.Dispose();
                }
                m_Streams.Clear();
            }

            s_UpdateThread.Remove(this);
        }

        void ReadPackets()
        {
            Profiler.BeginSample(nameof(ReadPackets));

            try
            {
                lock (m_StreamsLock)
                {
                    for (var i = 0; i < m_Streams.Count;)
                    {
                        var stream = m_Streams[i];

                        try
                        {
                            stream.ReadAllPackets();
                        }
                        catch (InvalidPacketSizeException e)
                        {
                            Debug.LogWarning($"{GetType().Name} running on port {Port} encountered an invalid packet length of {e.Size}. This may indicate that the stream type should be changed.");
                        }
                        catch (Exception)
                        {
                            m_Streams.RemoveAt(i);
                            stream.Dispose();
                            continue;
                        }

                        i++;
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        void UpdateConnections()
        {
            Profiler.BeginSample(nameof(UpdateConnections));

            try
            {
                // it is moderately expensive to check, so we check periodically instead of checking every update
                var pollTime = DateTime.Now;

                if (m_LastPollTime.HasValue && pollTime >= m_LastPollTime.Value && (pollTime - m_LastPollTime.Value) < k_PollPeriod)
                    return;

                m_LastPollTime = pollTime;

                lock (m_StreamsLock)
                {
                    for (var i = 0; i < m_Streams.Count;)
                    {
                        var stream = m_Streams[i];
                        var socket = (stream.Stream as SocketStream).Socket;

                        if (!socket.IsConnected())
                        {
                            m_Streams.RemoveAt(i);
                            stream.Dispose();
                            continue;
                        }

                        i++;
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        async Task ListenAsync(int port, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var listener = default(Socket);

                try
                {
                    if (TryCreateSocket(port, out listener, out m_ListenerException))
                    {
                        m_Listening = true;
                        await AcceptAsync(listener, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(k_RetrySocketPeriod, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    m_Listening = false;
                    listener?.Dispose();
                }
            }
        }

        async Task AcceptAsync(Socket listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var socket = default(Socket);

                try
                {
                    // get the socket for the new connection
                    var acceptTask = listener.AcceptAsync();
                    var cancelTask = cancellationToken.WhenCancelled();

                    await Task.WhenAny(acceptTask, cancelTask);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        if (acceptTask.IsCompleted)
                        {
                            acceptTask.Result?.Dispose();
                        }
                        return;
                    }

                    socket = acceptTask.Result;

                    // If a connection is idle for a long time, it may be closed by routers/firewalls.
                    // This option ensures that the connection keeps active. It also lets us know when
                    // the connection is terminated ungracefully.
                    socket.SetKeepAlive(true, k_Timeout, 1000);

                    // Create a stream reader used to decode the packet stream. The stream owns the socket,
                    // so the socket will be disposed when the stream is closed.
                    var origin = socket.RemoteEndPoint?.ToString();
                    var socketStream = new SocketStream(socket, FileAccess.Read);

                    OscStreamReader streamReader = m_StreamType switch
                    {
                        OscStreamType.LengthPrefix => new LengthPrefixStreamReader(socketStream, (packet, size) =>
                        {
                            HandlePacket(packet, size, origin);
                        }),
                        OscStreamType.Slip => new SlipStreamReader(socketStream, (packet, size) =>
                        {
                            HandlePacket(packet, size, origin);
                        }),
                        _ => throw new ArgumentOutOfRangeException(),
                    };

                    lock (m_StreamsLock)
                    {
                        m_Streams.Add(streamReader);
                    }
                }
                catch (Exception e)
                {
                    socket?.Dispose();

                    if (e is ObjectDisposedException or OperationCanceledException)
                    {
                        return;
                    }

                    Debug.LogError($"{GetType().Name}: Failed to accept connection: {e}");
                }
            }
        }

        static bool TryCreateSocket(int port, out Socket socket, out Exception ex)
        {
            socket = default;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // bind the socket so that it receives connections from all interfaces
                socket.Bind(new IPEndPoint(IPAddress.Any, port));

                // socket listening for connections
                socket.Listen(k_MaxPendingConnections);

                ex = null;
                return true;
            }
            catch (Exception e)
            {
                if (socket != null)
                {
                    socket.Dispose();
                    socket = null;
                }

                ex = e;
                return false;
            }
        }
    }
}
