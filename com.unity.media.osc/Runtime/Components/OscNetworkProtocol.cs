using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The transmission protocols supported for sending and receiving OSC messages via IP.
    /// </summary>
    public enum OscNetworkProtocol
    {
        /// <summary>
        /// The UDP transport protocol.
        /// </summary>
        [InspectorName("UDP")]
        Udp = 0,

        /// <summary>
        /// The TCP transport protocol.
        /// </summary>
        [InspectorName("TCP")]
        Tcp = 1,
    }
}
