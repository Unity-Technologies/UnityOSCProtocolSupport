using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class which uses UDP sockets to receive OSC Messages.
    /// </summary>
    /// <remarks>
    /// This implementation supports unicast, multicast, and broadcast.
    /// </remarks>
    public sealed class OscUdpServer : OscIpServer
    {
        const int k_DefaultBufferSize = 64 * 1024;

        readonly OscPacket m_Packet;
        IPAddress m_MulticastAddress;
        bool m_MulticastLoopback;
        Socket m_Socket;
        Exception m_SocketException;

        /// <summary>
        /// The IP address of the multicast group to join.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public IPAddress MulticastAddress
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscUdpServer));

                return m_MulticastAddress;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscUdpServer));

                if (!m_MulticastAddress?.Equals(value) ?? value != null)
                {
                    m_MulticastAddress = value;
                    RestartIfRunning();
                }
            }
        }

        /// <summary>
        /// Indicates whether this server receives multicast messages sent out from this device.
        /// </summary>
        /// <remarks>
        /// This only has an effect if <see cref="MulticastAddress"/> specifies a valid multicast address.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>
        public bool MulticastLoopback
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscUdpServer));

                return m_MulticastLoopback;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(OscUdpServer));

                if (m_MulticastLoopback != value)
                {
                    m_MulticastLoopback = value;
                    RestartIfRunning();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="OscUdpServer"/> instance.
        /// </summary>
        /// <param name="port">The networking port to listen for messages on.</param>
        /// <param name="bufferSize">The size of the buffer used to read messages. Must be large enough
        /// to fit the expected OSC Messages, otherwise the messages cannot be read.</param>
        public OscUdpServer(int port = OscConstants.DefaultPort, int bufferSize = k_DefaultBufferSize) : base(port)
        {
            m_Packet = new OscPacket(new byte[bufferSize]);
        }

        /// <inheritdoc />
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            m_Packet.Dispose();
        }

        /// <summary>
        /// Gets a string describing the server state.
        /// </summary>
        /// <returns>A string representing the server state.</returns>
        public override string ToString()
        {
            return $"UDP ({Port})";
        }

        /// <inheritdoc />
        public override Status GetStatus(out string message)
        {
            var status = base.GetStatus(out message);

            if (status == Status.Ok)
            {
                if (m_SocketException != null)
                {
                    message = m_SocketException.Message;
                    status = Status.Error;
                }
            }

            return status;
        }

        /// <inheritdoc />
        protected override bool OnStart()
        {
            if (!TryCreateSocket(out m_Socket, out m_SocketException))
                return false;

            // start receiving messages from the socket
            var receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.SetBuffer(m_Packet.Buffer, 0, m_Packet.Buffer.Length);
            receiveArgs.Completed += ReceiveComplete;
            receiveArgs.UserToken = m_Socket;
            receiveArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            BeginReceive(receiveArgs);

            return true;
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            if (m_Socket != null)
            {
                m_Socket.Dispose();
                m_Socket = null;
            }
        }

        void BeginReceive(SocketAsyncEventArgs args)
        {
            var socket = args.UserToken as Socket;

            // This returns false if the completion callback completed synchronously, so we must manually
            // call the completion handler in that case.
            if (!socket.ReceiveFromAsync(args))
            {
                ReceiveComplete(socket, args);
            }
        }

        void ReceiveComplete(object sender, SocketAsyncEventArgs args)
        {
            try
            {
                switch (args.SocketError)
                {
                    case SocketError.Success:
                        break;

                    // if we are not connected yet, keep waiting until we are connected
                    case SocketError.NotConnected:
                        BeginReceive(args);
                        return;

                    // This error indicates a datagram was not received and an ICMP "Port Unreachable"
                    // response was received. We don't care if a UDP packet was not received, so don't
                    // close the connection in that case.
                    case SocketError.ConnectionReset:
                        break;

                    // When the socket is disposed, suppress the error since it is expected that
                    // the operation should not complete.
                    case SocketError.Shutdown:
                    case SocketError.Interrupted:
                    case SocketError.OperationAborted:
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionRefused:
                    case SocketError.Disconnecting:
                        return;

                    default:
                        throw new SocketException((int)args.SocketError);
                }

                var packetOrigin = IsMonitorCallbackRegistered ? args.RemoteEndPoint?.ToString() : null;

                HandlePacket(m_Packet, args.BytesTransferred, packetOrigin);

                BeginReceive(args);
            }
            catch (ObjectDisposedException)
            {
                // suppress the exception thrown by this callback if the socket was disposed
            }
            catch (Exception e)
            {
                Debug.LogError($"{GetType().Name}: Failed to receive message: {e}");
            }
        }

        bool TryCreateSocket(out Socket socket, out Exception ex)
        {
            socket = default;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // ensure we can fit any message we want to receive with the socket
                socket.ReceiveBufferSize = m_Packet.Buffer.Length;

                // multiple OSC servers can share the same address/port without issue
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);

                // bind the socket so that it receives messages from all interfaces
                socket.Bind(new IPEndPoint(IPAddress.Any, Port));

                // join the multicast group if a multicast address is specified
                if (m_MulticastAddress != null && NetworkingUtils.IsMulticast(m_MulticastAddress))
                {
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(m_MulticastAddress));
                    socket.MulticastLoopback = m_MulticastLoopback;
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
