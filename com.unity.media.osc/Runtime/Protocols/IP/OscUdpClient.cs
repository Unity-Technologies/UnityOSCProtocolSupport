using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class which uses UDP sockets to send OSC Messages.
    /// </summary>
    /// <remarks>
    /// This implementation supports unicast, multicast, and broadcast.
    /// </remarks>
    public sealed class OscUdpClient : OscIpClient
    {
        /// <summary>
        /// This value approximates the largest possible UDP message.
        /// </summary>
        const int k_DefaultBufferSize = 64 * 1024;

        class SendState
        {
            public int SendBufferReferenceCount;
        }

        static readonly ConcurrentBag<SendState> s_SendStatePool = new ConcurrentBag<SendState>();
        static readonly ConcurrentBag<SocketAsyncEventArgs> s_SendArgsPool = new ConcurrentBag<SocketAsyncEventArgs>();

        IPAddress[] m_OverrideInterfaces;
        readonly List<Socket> m_Sockets = new List<Socket>();
        readonly object m_SocketsLock = new object();
        volatile bool m_RestartSockets;

        /// <summary>
        /// Overrides the default selection of network interfaces used to send out messages.
        /// </summary>
        /// <remarks>
        /// By default, the client sends out messages on all available interfaces except the loopback interface.
        /// Use this option to specify a custom set of interfaces to use instead. Set to <see langword="null"/> to
        /// restore the default behavior.
        /// </remarks>
        public IPAddress[] OverrideInterfaces
        {
            get => m_OverrideInterfaces?.ToArray();
            set
            {
                m_OverrideInterfaces = value?
                    .Where(address => address != null)
                    .Distinct()
                    .ToArray();

                RestartIfRunning();
            }
        }

        /// <summary>
        /// Creates a new <see cref="OscUdpClient"/> instance.
        /// </summary>
        /// <param name="bufferSize">The size of the buffer used to write and send messages. Must be large enough
        /// to fit the OSC Messages to send, otherwise the messages cannot be sent.</param>
        public OscUdpClient(int bufferSize = k_DefaultBufferSize) : this(new IPEndPoint(IPAddress.Broadcast, OscConstants.DefaultPort), bufferSize)
        {
        }

        /// <summary>
        /// Creates a new <see cref="OscUdpClient"/> instance.
        /// </summary>
        /// <param name="endPoint">The IP address and port to send messages to.</param>
        /// <param name="bufferSize">The size of the buffer used to write and send messages. Must be large enough
        /// to fit the OSC Messages to send, otherwise the messages cannot be sent.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endPoint"/> is null.</exception>
        public OscUdpClient(IPEndPoint endPoint, int bufferSize = k_DefaultBufferSize) : base(endPoint, bufferSize)
        {
        }

        /// <summary>
        /// Gets a string describing the client state.
        /// </summary>
        /// <returns>A string representing the client state.</returns>
        public override string ToString()
        {
            return $"UDP ({EndPoint})";
        }

        /// <inheritdoc />
        public override Status GetStatus(out string message)
        {
            var status = base.GetStatus(out message);

            if (status == Status.Ok)
            {
                lock (m_SocketsLock)
                {
                    if (m_Sockets.Count <= 0)
                    {
                        message = "No available network interfaces.";
                        status = Status.Warning;
                    }
                }
            }

            return status;
        }

        /// <inheritdoc />
        protected override bool OnStart()
        {
            m_RestartSockets = false;

            lock (m_SocketsLock)
            {
                foreach (var interfaceAddress in GetInterfaceAddresses())
                {
                    if (TryCreateSocket(interfaceAddress, out var socket, out _))
                    {
                        m_Sockets.Add(socket);
                    }
                }
            }

            OscManager.PreUpdate += OnUpdate;

            return true;
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            OscManager.PreUpdate -= OnUpdate;

            lock (m_SocketsLock)
            {
                foreach (var socket in m_Sockets)
                {
                    socket.Dispose();
                }

                m_Sockets.Clear();
            }
        }

        void OnUpdate()
        {
            if (m_RestartSockets)
            {
                RestartIfRunning();
            }
        }

        /// <inheritdoc />
        protected override void OnSendPacket(byte[] buffer, int size)
        {
            // Copy the message into a pooled send buffer. We can share this buffer
            // with all sockets we are sending on. However, we must be careful to
            // only return the buffer to the pool once after all sockets are finished
            // with it. We can use reference counting to do this.
            var sendBuffer = ArrayPool<byte>.Shared.Rent(size);

            unsafe
            {
                fixed (byte* srcPtr = buffer)
                fixed (byte* dstPtr = sendBuffer)
                {
                    UnsafeUtility.MemCpy(dstPtr, srcPtr, size);
                }
            }

            if (!s_SendStatePool.TryTake(out var state))
            {
                state = new SendState();
            }

            // send the message on all sockets
            lock (m_SocketsLock)
            {
                state.SendBufferReferenceCount = m_Sockets.Count;

                foreach (var socket in m_Sockets)
                {
                    try
                    {
                        // prepare the sending arguments
                        if (!s_SendArgsPool.TryTake(out var args))
                        {
                            args = new SocketAsyncEventArgs();
                            args.Completed += SendComplete;
                            args.UserToken = state;
                        }

                        args.RemoteEndPoint = EndPoint;
                        args.SetBuffer(sendBuffer, 0, size);

                        // if the send operation completion synchronously, we must manually call the completion handler
                        if (!socket.SendToAsync(args))
                        {
                            SendComplete(socket, args);
                        }
                    }
                    catch (Exception e)
                    {
                        DecrementReferenceCount(state, sendBuffer);

                        Debug.LogError($"{GetType().Name}: Failed to send message: {e}");
                    }
                }
            }
        }

        void SendComplete(object sender, SocketAsyncEventArgs args)
        {
            try
            {
                switch (args.SocketError)
                {
                    case SocketError.Success:
                        return;

                    // When the socket is disposed, suppress the error since it is expected that
                    // the operation should not complete.
                    case SocketError.Shutdown:
                    case SocketError.Interrupted:
                    case SocketError.OperationAborted:
                    case SocketError.ConnectionAborted:
                    case SocketError.Disconnecting:
                        return;

                    // This error indicates a datagram was not received and an ICMP "Port Unreachable"
                    // response was received. We don't care if a UDP packet was not received, so don't
                    // close the connection in that case.
                    case SocketError.ConnectionReset:
                        return;

                    // If we can't send on this network interface, the available networks may have changed.
                    // In this case, we should try to re-initialize using the current interfaces.
                    case SocketError.AddressNotAvailable:
                    case SocketError.HostUnreachable:
                    case SocketError.NetworkUnreachable:
                    case SocketError.NetworkDown:
                        m_RestartSockets = true;
                        return;

                    default:
                        throw new SocketException((int)args.SocketError);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{GetType().Name}: Failed to send message: {e}");
            }
            finally
            {
                s_SendArgsPool.Add(args);

                DecrementReferenceCount(args.UserToken as SendState, args.Buffer);
            }
        }

        static void DecrementReferenceCount(SendState state, byte[] buffer)
        {
            // if this is the last socket to complete sending the message, return the message buffer to the pool
            var referenceCount = Interlocked.Decrement(ref state.SendBufferReferenceCount);

            if (referenceCount == 0)
            {
                s_SendStatePool.Add(state);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        IPAddress[] GetInterfaceAddresses()
        {
            // if the destination is the loopback address, only send via the loopback interface
            if (EndPoint.Address.Equals(IPAddress.Loopback))
            {
                return new[] { IPAddress.Loopback };
            }

            return m_OverrideInterfaces ?? NetworkingUtils.GetIPAddresses(false);
        }

        bool TryCreateSocket(IPAddress localInterface, out Socket socket, out Exception ex)
        {
            socket = default;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // ensure we can fit any message we want to send with the socket
                socket.SendBufferSize = BufferSize;

                // multiple clients can share the same address/port without issue
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);

                // support broadcasting if required
                var ipAddress = EndPoint.Address;
                var isBroadcast = ipAddress.Equals(IPAddress.Broadcast);

                if (isBroadcast)
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                }

                // bind the socket the the interface
                socket.Bind(new IPEndPoint(localInterface, 0));

                // We should check that broadcasting is supported on the selected interface if needed.
                // This can be done by trying to broadcast and checking if an error is thrown.
                if (isBroadcast)
                {
                    socket.SendTo(new byte[] { }, EndPoint);
                }

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
