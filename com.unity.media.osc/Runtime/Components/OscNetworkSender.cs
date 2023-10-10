using System.Net;
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// Use this component to send OSC Messages via IP.
    /// </summary>
    [AddComponentMenu("OSC/OSC Sender")]
    public sealed class OscNetworkSender : OscSender
    {
        /// <summary>
        /// Gets if <see cref="OscNetworkSender"/> is supported on the current platform.
        /// </summary>
        /// <returns><see langword="true"/> if <see cref="OscNetworkSender"/> is supported on the current platform; otherwise, <see langword="false"/>.</returns>
        public static bool IsSupported()
        {
#if UNITY_STANDALONE || UNITY_IPHONE || UNITY_ANDROID || UNITY_WSA
            return true;
#else
            return false;
#endif
        }

        [SerializeField, Tooltip("The network protocol used to send OSC Messages.")]
        OscNetworkProtocol m_Protocol = OscNetworkProtocol.Udp;
        [SerializeField, Tooltip("The destination IP address to send OSC Messages to.")]
        string m_IpAddress = IPAddress.Broadcast.ToString();
        [SerializeField, Tooltip("The destination port to send OSC Messages to.")]
        int m_Port = 8000;
        [SerializeField, Tooltip("The framing to use for the TCP packet stream. This must match the configuration of the receiving application." +
             "\n\nUse length prefix when sending to applications implementing the OSC 1.0 specification." +
             "\n\nUse SLIP when sending to applications implementing the OSC 1.1 specification.")]
        OscStreamType m_StreamType = OscStreamType.Slip;
        [SerializeField, Tooltip("Automatically group messages into OSC Bundles. " +
             "This may help to reduce network overhead due to sending lots of smaller messages. " +
             "However, the receiving device or application must support OSC Bundles.")]
        bool m_AutoBundleMessages = false;

        /// <summary>
        /// The network protocol used to send OSC Messages.
        /// </summary>
        public OscNetworkProtocol Protocol
        {
            get => m_Protocol;
            set
            {
                if (m_Protocol != value)
                {
                    m_Protocol = value;
                    UpdateClient();
                }
            }
        }

        /// <summary>
        /// The destination IP address to send OSC Messages to.
        /// </summary>
        public string IpAddress
        {
            get => m_IpAddress;
            set
            {
                var newValue = value ?? string.Empty;

                if (m_IpAddress != newValue)
                {
                    m_IpAddress = newValue;
                    UpdateClient();
                }
            }
        }

        /// <summary>
        /// The destination port to send OSC Messages to.
        /// </summary>
        public int Port
        {
            get => m_Port;
            set
            {
                if (m_Port != value)
                {
                    m_Port = value;
                    UpdateClient();
                }
            }
        }

        /// <summary>
        /// The framing to use for the TCP packet stream.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="Protocol"/> is set to <see cref="OscNetworkProtocol.Tcp"/>. The stream type must
        /// match the configuration of the receiving application in order for the stream to be read correctly by the receiving application.
        /// </remarks>
        public OscStreamType StreamType
        {
            get => m_StreamType;
            set
            {
                if (m_StreamType != value)
                {
                    m_StreamType = value;
                    UpdateClient();
                }
            }
        }

        void OnValidate()
        {
            UpdateClient();
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateClient();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();

            SetClient(null);
        }

        void UpdateClient()
        {
            if (!IsSupported() || !isActiveAndEnabled)
            {
                SetClient(null);
                return;
            }

            // only use a client if the configuration is valid
            if (!IPAddress.TryParse(m_IpAddress, out var address) || !NetworkingUtils.IsPortValid(m_Port, out _))
            {
                SetClient(null);
                return;
            }

            switch (m_Protocol)
            {
                case OscNetworkProtocol.Udp:
                {
                    if (!TryGetClient<OscUdpClient>(out var udpClient))
                    {
                        udpClient = new OscUdpClient();
                        SetClient(udpClient);
                    }
                    break;
                }
                case OscNetworkProtocol.Tcp:
                {
                    if (!TryGetClient<OscTcpClient>(out var tcpClient))
                    {
                        tcpClient = new OscTcpClient();
                        SetClient(tcpClient);
                    }

                    tcpClient.StreamType = m_StreamType;
                    break;
                }
            }

            if (TryGetClient<OscIpClient>(out var ipClient))
            {
                ipClient.EndPoint = new IPEndPoint(address, m_Port);
                ipClient.AutoBundle = m_AutoBundleMessages;
                ipClient.Start();
            }
        }

        /// <inheritdoc />
        protected override void OnClientChange(OscClient oldClient, OscClient newClient)
        {
            if (oldClient != null)
            {
                oldClient.Dispose();
            }
        }
    }
}
