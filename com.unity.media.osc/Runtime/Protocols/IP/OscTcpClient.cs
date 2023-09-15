using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class which uses TCP sockets to send OSC Messages.
    /// </summary>
    public sealed class OscTcpClient : OscIpClient
    {
        const int k_DefaultBufferSize = 64 * 1024;

        /// <summary>
        /// The time in milliseconds after which the connection should be closed if the server cannot be reached.
        /// </summary>
        const int k_Timeout = 10 * 1000;

        /// <summary>
        /// The time between polling the socket to check if the connection is still active.
        /// </summary>
        static readonly TimeSpan k_PollPeriod = TimeSpan.FromSeconds(1.0);

        static UpdateThread<OscTcpClient> s_UpdateThread = new("OSC", nameof(OscTcpClient), client =>
        {
            client.SendPackets();
            client.UpdateConnection();
        });

        static OscTcpClient()
        {
            OscManager.NetworkUpdate += () => s_UpdateThread.QueueUpdate();
        }

        OscStreamType m_StreamType = OscStreamType.Slip;
        readonly ConcurrentQueue<(byte[] buffer, int size)> m_SendQueue = new ConcurrentQueue<(byte[], int)>();
        CancellationTokenSource m_ConnectCancellationTokenSource;
        OscStreamWriter m_Stream;
        Exception m_SocketException;
        DateTime? m_LastPollTime;

        /// <summary>
        /// The time duration in milliseconds to wait between connection attempts.
        /// </summary>
        public int ConnectAttemptTimeout { get; set; } = 2000;

        /// <summary>
        /// The framing used in the packet stream.
        /// </summary>
        /// <remarks>
        /// The stream type must match the configuration of the receiving application in order for the stream to be read correctly by the receiving application.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public OscStreamType StreamType
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscTcpClient));

                return m_StreamType;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscTcpClient));

                if (m_StreamType != value)
                {
                    m_StreamType = value;
                    RestartIfRunning();
                }
            }
        }

        /// <inheritdoc />
        public override bool IsReady => base.IsReady && m_Stream != null;

        /// <summary>
        /// Creates a new <see cref="OscTcpClient"/> instance.
        /// </summary>
        /// <param name="bufferSize">The size of the buffer used to write and send messages. Must be large enough
        /// to fit the OSC Messages to send, otherwise the messages cannot be sent.</param>
        public OscTcpClient(int bufferSize = k_DefaultBufferSize) : this(new IPEndPoint(IPAddress.None, OscConstants.DefaultPort), bufferSize)
        {
        }

        /// <summary>
        /// Creates a new <see cref="OscTcpClient"/> instance.
        /// </summary>
        /// <param name="endPoint">The IP address and port to send messages to.</param>
        /// <param name="bufferSize">The size of the buffer used to write and send messages. Must be large enough
        /// to fit the OSC Messages to send, otherwise the messages cannot be sent.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endPoint"/> is null.</exception>
        public OscTcpClient(IPEndPoint endPoint, int bufferSize) : base(endPoint, bufferSize)
        {

        }

        /// <summary>
        /// Gets a string describing the client state.
        /// </summary>
        /// <returns>A string representing the client state.</returns>
        public override string ToString()
        {
            return $"TCP ({EndPoint}, {m_StreamType.GetDisplayName()})";
        }

        /// <inheritdoc />
        public override Status GetStatus(out string message)
        {
            var status = base.GetStatus(out message);

            if (status == Status.Ok)
            {
                if (m_Stream != null)
                {
                    message = "Connected.";
                    status = Status.Ok;
                }
                else if (m_SocketException != null)
                {
                    message = m_SocketException.Message;
                    status = Status.Error;
                }
                else
                {
                    message = "Connecting...";
                    status = Status.Warning;
                }
            }

            return status;
        }

        /// <inheritdoc />
        protected override bool OnStart()
        {
            m_LastPollTime = null;
            m_ConnectCancellationTokenSource = new CancellationTokenSource();

            _ = ConnectLoopAsync(IPAddress.Any, EndPoint, m_ConnectCancellationTokenSource.Token);

            s_UpdateThread.Add(this);
            return true;
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            // stop attempting to connect
            if (m_ConnectCancellationTokenSource != null)
            {
                m_ConnectCancellationTokenSource.Cancel();
                m_ConnectCancellationTokenSource = null;
            }

            // close the connection
            if (m_Stream != null)
            {
                m_Stream.Dispose();
                m_Stream = null;
            }

            // empty the packet queue
            while (m_SendQueue.TryDequeue(out var packet))
            {
                ArrayPool<byte>.Shared.Return(packet.buffer);
            }

            s_UpdateThread.Remove(this);
        }

        /// <inheritdoc />
        protected override void OnSendPacket(byte[] buffer, int size)
        {
            var sendBuffer = ArrayPool<byte>.Shared.Rent(size);

            unsafe
            {
                fixed (byte* srcPtr = buffer)
                fixed (byte* dstPtr = sendBuffer)
                {
                    UnsafeUtility.MemCpy(dstPtr, srcPtr, size);
                }
            }

            m_SendQueue.Enqueue((sendBuffer, size));
        }

        void SendPackets()
        {
            Profiler.BeginSample(nameof(SendPackets));

            try
            {
                if (m_Stream == null)
                    return;

                while (m_SendQueue.TryDequeue(out var packet))
                {
                    try
                    {
                        m_Stream.WriteToStream(packet.buffer, packet.size);
                    }
                    catch (Exception)
                    {
                        RestartIfRunning();
                        return;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(packet.buffer);
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        void UpdateConnection()
        {
            Profiler.BeginSample(nameof(UpdateConnection));

            try
            {
                if (m_Stream == null)
                    return;

                // it is moderately expensive to check, so we check periodically instead of checking every update
                var pollTime = DateTime.Now;

                if (m_LastPollTime.HasValue && pollTime >= m_LastPollTime.Value && (pollTime - m_LastPollTime.Value) < k_PollPeriod)
                    return;

                m_LastPollTime = pollTime;

                // if the socket becomes disconnected, attempt reconnection
                if (m_Stream.Stream is SocketStream socketStream && !socketStream.Socket.IsConnected())
                {
                    RestartIfRunning();
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        async Task ConnectLoopAsync(IPAddress localInterface, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // restart connection attempts periodically
                    var timeout = Task.Delay(ConnectAttemptTimeout, cancellationToken);

                    // if we can't create the socket for this attempt, retry after the timeout
                    if (!TryCreateSocket(localInterface, out var socket, out m_SocketException))
                    {
                        await timeout;
                        continue;
                    }

                    // If we can create the socket, start a connection attempt and wait
                    // until either the attempt succeeds/fails or the timeout ends. We cannot use
                    // Socket.ConnectAsync, it causes a freeze on MacOS/Linux.
                    var connectAttempt = ConnectAttemptAsync(socket, remoteEndPoint);

                    await Task.WhenAny(timeout, connectAttempt);

                    // if the connection attempt failed try again
                    if (!socket.Connected)
                    {
                        socket.Dispose();
                        continue;
                    }

                    // Create a stream writer used to encode the packet stream. The stream owns the socket,
                    // so the socket will be disposed when the stream is closed.
                    var socketStream = new SocketStream(socket, FileAccess.Write);

                    m_Stream = m_StreamType switch
                    {
                        OscStreamType.LengthPrefix => new LengthPrefixStreamWriter(socketStream),
                        OscStreamType.Slip => new SlipStreamWriter(socketStream),
                        _ => null,
                    };

                    return;
                }
                catch (OperationCanceledException)
                {
                    // raised when the connection attempt is cancelled
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"{GetType().Name}: Failed to connect: {e}");
                }
            }
        }

        static Task ConnectAttemptAsync(Socket socket, IPEndPoint remoteEndPoint)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            try
            {
                socket.BeginConnect(remoteEndPoint, result =>
                {
                    try
                    {
                        socket.EndConnect(result);
                        taskCompletionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }, socket);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }

            return taskCompletionSource.Task;
        }

        static bool TryCreateSocket(IPAddress localInterface, out Socket socket, out Exception ex)
        {
            socket = default;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Disable Nagle's Algorithm. This helps to reduce latency when fewer, smaller message are being sent.
                socket.NoDelay = true;

                // If a connection is idle for a long time, it may be closed by routers/firewalls.
                // This option ensures that the connection keeps active. It also lets us know when
                // the connection is terminated ungracefully.
                socket.SetKeepAlive(true, k_Timeout, 1000);

                // By default tcp sockets will persist after being closed in order to ensure all
                // data has been send and received successfully, but this will block the port for a while.
                // We need to disable this behaviour so the socket closes immediately.
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                socket.LingerState = new LingerOption(true, 0);

                // bind the socket the the interface
                socket.Bind(new IPEndPoint(localInterface, 0));

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
