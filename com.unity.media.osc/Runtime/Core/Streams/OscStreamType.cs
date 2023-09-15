using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// The standard stream framing encodings for OSC packets.
    /// </summary>
    public enum OscStreamType
    {
        /// <summary>
        /// Packet length prefix is used by OSC streams implementing the OSC 1.0 specification.
        /// </summary>
        [InspectorName("Length Prefix")]
        LengthPrefix = 0,

        /// <summary>
        /// SLIP (RFC 1055) is used by OSC streams implementing the OSC 1.1 specification.
        /// </summary>
        [InspectorName("SLIP")]
        Slip = 1,
    }
}
