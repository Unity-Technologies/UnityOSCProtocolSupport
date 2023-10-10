using System;
using System.Net;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// Use this component to receive OSC Messages sent via IP.
    /// </summary>
    [AddComponentMenu("OSC/OSC Receiver")]
    public sealed class OscNetworkReceiver : OscReceiver
    {
        readonly struct UdpConfig : IEquatable<UdpConfig>
        {
            public int Port { get; }
            public IPAddress MulticastAddress { get; }
            public bool MulticastLoopback { get; }

            public UdpConfig(int port, IPAddress multicastAddress, bool multicastLoopback)
            {
                Port = port;
                MulticastAddress = multicastAddress;
                MulticastLoopback = multicastLoopback;
            }

            /// <inheritdoc />
            public bool Equals(UdpConfig other)
            {
                return Port == other.Port
                    && Equals(MulticastAddress, other.MulticastAddress)
                    && MulticastLoopback == other.MulticastLoopback;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is UdpConfig other && Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Port;
                    hashCode = (hashCode * 397) ^ (MulticastAddress != null ? MulticastAddress.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ MulticastLoopback.GetHashCode();
                    return hashCode;
                }
            }
        }

        // To reduce overhead we only create one server per each server configuration and share that instance with
        // receivers using that same configuration.
        static readonly ReferenceManager<UdpConfig, OscNetworkReceiver, OscUdpServer> s_UdpServers = new(config =>
        {
            var server = new OscUdpServer
            {
                Port = config.Port,
                MulticastAddress = config.MulticastAddress,
                MulticastLoopback = config.MulticastLoopback,
            };
            return server;
        });
        static readonly ReferenceManager<int, OscNetworkReceiver, OscTcpServer> s_TcpServers = new(port =>
        {
            var server = new OscTcpServer
            {
                Port = port,
            };
            return server;
        });

        /// <summary>
        /// Gets if <see cref="OscNetworkReceiver"/> is supported on the current platform.
        /// </summary>
        /// <returns><see langword="true"/> if <see cref="OscNetworkReceiver"/> is supported on the current platform; otherwise, <see langword="false"/>.</returns>
        public static bool IsSupported()
        {
#if UNITY_STANDALONE || UNITY_IPHONE || UNITY_ANDROID || UNITY_WSA
            return true;
#else
            return false;
#endif
        }

        [SerializeField, Tooltip("The network protocol to receive incoming OSC messages with.")]
        OscNetworkProtocol m_Protocol = OscNetworkProtocol.Udp;
        [SerializeField, Tooltip("The local port on which to listen for incoming OSC messages.")]
        int m_Port = 8000;
        [SerializeField, Tooltip("Enables the reception of OSC messages sent via UDP multicast.")]
        bool m_ReceiveMulticast;
        [SerializeField, Tooltip("The IP address of the UDP multicast group to join.")]
        string m_MulticastAddress = IPAddress.Any.ToString();
        [SerializeField, Tooltip("Enables the reception of UDP multicast messages sent out by the local device.")]
        bool m_MulticastLoopback;
        [SerializeField, Tooltip("The framing used in the TCP packet stream. This must match the configuration of the sending application." +
             "\n\nUse length prefix when receiving from applications implementing the OSC 1.0 specification." +
             "\n\nUse SLIP when receiving from applications implementing the OSC 1.1 specification.")]
        OscStreamType m_StreamType = OscStreamType.Slip;

        /// <summary>
        /// The network protocol used for incoming OSC Messages.
        /// </summary>
        public OscNetworkProtocol Protocol
        {
            get => m_Protocol;
            set
            {
                if (m_Protocol != value)
                {
                    m_Protocol = value;
                    UpdateServer();
                }
            }
        }

        /// <summary>
        /// The local port to listen on for incoming OSC Messages.
        /// </summary>
        public int Port
        {
            get => m_Port;
            set
            {
                if (m_Port != value)
                {
                    m_Port = value;
                    UpdateServer();
                }
            }
        }

        /// <summary>
        /// Indicates whether to receive OSC Messages sent via UDP multicast.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="Protocol"/> is set to <see cref="OscNetworkProtocol.Udp"/>.
        /// </remarks>
        public bool ReceiveMulticast
        {
            get => m_ReceiveMulticast;
            set
            {
                if (m_ReceiveMulticast != value)
                {
                    m_ReceiveMulticast = value;
                    UpdateServer();
                }
            }
        }

        /// <summary>
        /// The IP address of the UDP multicast group to join.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="Protocol"/> is set to <see cref="OscNetworkProtocol.Udp"/>
        /// and <see cref="ReceiveMulticast"/> is <see langword="true"/>.
        /// </remarks>
        public string MulticastAddress
        {
            get => m_MulticastAddress;
            set
            {
                var newValue = value ?? string.Empty;

                if (m_MulticastAddress != newValue)
                {
                    m_MulticastAddress = newValue;
                    UpdateServer();
                }
            }
        }

        /// <summary>
        /// Indicates whether to receive UDP multicast messages sent out by the local device.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="Protocol"/> is set to <see cref="OscNetworkProtocol.Udp"/>
        /// and <see cref="ReceiveMulticast"/> is <see langword="true"/>.
        /// </remarks>
        public bool MulticastLoopback
        {
            get => m_MulticastLoopback;
            set
            {
                if (m_MulticastLoopback != value)
                {
                    m_MulticastLoopback = value;
                    UpdateServer();
                }
            }
        }

        /// <summary>
        /// The framing used in the TCP packet stream.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="Protocol"/> is set to <see cref="OscNetworkProtocol.Tcp"/>. The stream type must
        /// match the configuration of the sending application in order for the stream to be read correctly. If multiple receivers
        /// are using the same port but specify different stream types, the first receiver to start will control the stream type used
        /// by all the receivers on that port.
        /// </remarks>
        public OscStreamType StreamType
        {
            get => m_StreamType;
            set
            {
                if (m_StreamType != value)
                {
                    m_StreamType = value;
                    UpdateServer();
                }
            }
        }

        void OnValidate()
        {
            UpdateServer();
        }

        void OnEnable()
        {
            UpdateServer();
        }

        void OnDisable()
        {
            SetServer(null);
        }

        void UpdateServer()
        {
            if (!IsSupported() || !isActiveAndEnabled)
            {
                SetServer(null);
                return;
            }

            switch (m_Protocol)
            {
                case OscNetworkProtocol.Udp:
                {
                    // determine the requested server configuration
                    var multicastAddress = default(IPAddress);
                    var multicastLoopback = false;

                    if (m_ReceiveMulticast && IPAddress.TryParse(m_MulticastAddress, out multicastAddress))
                    {
                        multicastLoopback = m_MulticastLoopback;
                    }

                    var config = new UdpConfig(m_Port, multicastAddress, multicastLoopback);

                    // if the current server doesn't match the requested configuration we should
                    // switch to a server with a matching configuration
                    if (!TryGetServer<OscUdpServer>(out var udpServer) || !GetUdpServerConfig(udpServer).Equals(config))
                    {
                        s_UdpServers.GetOrCreate(config, this, out udpServer);
                        SetServer(udpServer);
                    }

                    break;
                }
                case OscNetworkProtocol.Tcp:
                {
                    // if the current server isn't running on the desired port we should
                    // switch to a server with a matching port
                    if (!TryGetServer<OscTcpServer>(out var tcpServer) || tcpServer.Port != m_Port)
                    {
                        s_TcpServers.GetOrCreate(m_Port, this, out tcpServer);
                        SetServer(tcpServer);
                    }

                    // only allow setting the configuration if all receivers using the server instance have the same configuration
                    var canConfigure = true;

                    foreach (var receiver in s_TcpServers.GetOwners(m_Port))
                    {
                        if (receiver.m_StreamType != m_StreamType)
                        {
                            canConfigure = false;
                            break;
                        }
                    }

                    if (canConfigure)
                    {
                        tcpServer.StreamType = m_StreamType;
                    }

                    break;
                }
            }

            if (TryGetServer<OscIpServer>(out var ipServer))
            {
                ipServer.Start();
            }
        }

        /// <inheritdoc />
        protected override void OnServerChange(OscServer oldServer, OscServer newServer)
        {
            switch (oldServer)
            {
                case OscUdpServer udpServer:
                {
                    if (!udpServer.IsDisposed)
                    {
                        s_UdpServers.Release(GetUdpServerConfig(udpServer), this);
                    }
                    break;
                }
                case OscTcpServer tcpServer:
                {
                    if (!tcpServer.IsDisposed)
                    {
                        s_TcpServers.Release(tcpServer.Port, this);
                    }
                    break;
                }
            }
        }

        static UdpConfig GetUdpServerConfig(OscUdpServer server)
        {
            return new UdpConfig(
                server.Port,
                server.MulticastAddress,
                server.MulticastLoopback
            );
        }
    }
}
