using System;
using System.Text;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class that contains OSC related constants.
    /// </summary>
    public static class OscConstants
    {
        /// <summary>
        /// The default port to use for OSC servers and clients.
        /// </summary>
        public const int DefaultPort = 8900;

        /// <summary>
        /// The smallest length of a valid OSC Address or OSC Address Pattern.
        /// </summary>
        public const int MinAddressLength = 2;

        /// <summary>
        /// The string used to indicate the start of an OSC bundle.
        /// </summary>
        public const string BundlePrefix = "#bundle\0";

        /// <summary>
        /// The string used to indicate the start of an OSC bundle as a long.
        /// </summary>
        internal static readonly long BundlePrefixLong;

        static OscConstants()
        {
            var bundleBytes = Encoding.ASCII.GetBytes(BundlePrefix);
            BundlePrefixLong = BitConverter.ToInt64(bundleBytes, 0);
        }
    }
}
