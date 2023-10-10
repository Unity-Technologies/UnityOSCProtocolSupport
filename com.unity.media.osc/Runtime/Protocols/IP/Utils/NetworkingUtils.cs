using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Unity.Media.Osc
{
    static class NetworkingUtils
    {
        /// <summary>
        /// Get the online IPv4 addresses from all network interfaces in the system.
        /// </summary>
        /// <param name="includeLoopback">Include any addresses on the loopback interface.</param>
        /// <returns>A new array containing the available IP addresses.</returns>
        public static IPAddress[] GetIPAddresses(bool includeLoopback)
        {
            var addresses = new List<IPAddress>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!includeLoopback && networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                switch (networkInterface.OperationalStatus)
                {
                    case OperationalStatus.Up:
                        break;
                    case OperationalStatus.Unknown:
                        // On Linux the loopback interface reports as unknown status, so we get it anyways
                        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                            break;
                        continue;
                    default:
                        continue;
                }

                foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    var address = ip.Address;

                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        addresses.Add(address);
                    }
                }
            }

            return addresses.ToArray();
        }

        /// <summary>
        /// Checks if an IP address is a multicast address.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns><see langword="true"/> if <paramref name="ipAddress"/> is a multicast address; otherwise, false.</returns>
        public static bool IsMulticast(IPAddress ipAddress)
        {
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                {
                    // Multicast addresses range from 224.0.0.0 to 239.255.255.255
                    // We can just check the first byte to see if an address is in the
                    // multicast range.
                    return (ipAddress.GetAddressBytes()[0] & 0xF0) == 0xE0;
                }
                case AddressFamily.InterNetworkV6:
                {
                    return ipAddress.IsIPv6Multicast;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a port number is valid to use.
        /// </summary>
        /// <param name="port">The port number to check.</param>
        /// <param name="message">Returns null if the port is suitable, otherwise it returns
        /// a message explaining why the port is not valid or recommended. May contain a value even
        /// if this method returns true.</param>
        /// <returns>True if the port number is strictly valid.</returns>
        public static bool IsPortValid(int port, out string message)
        {
            message = null;

            if (port < 0)
            {
                message = "Port numbers cannot be negative.";
                return false;
            }
            if (port == 0)
            {
                message = "Port 0 is a reserved port and cannot be used.";
                return false;
            }
            if (port > 65535)
            {
                message = "Port numbers cannot be larger than 65535.";
                return false;
            }

            if (port < 1024)
            {
                message = "Ports on range [1, 1023] are reserved for well-known services and should not be used if possible.";
                return true;
            }
            if (port >= 32768)
            {
                // This range encompasses the ephemeral port range used by all common OSs.
                // While this can be configured, the vast majority of systems use the defaults.
                message = "Ports on range [32768, 65535] are often used by the OS. It is recommended to use a port between 1024 and 32767.";
                return true;
            }

            return true;
        }

        /// <summary>
        /// Checks if a TCP socket is connected.
        /// </summary>
        /// <param name="socket">The socket to check.</param>
        /// <returns><see langword="true"/> if <paramref name="socket"/> is currently connected to the remote; otherwise, <see langword="false"/>.</returns>
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return socket.Connected && (socket.Available > 0 || !socket.Poll(100, SelectMode.SelectRead));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the keep alive parameters for a socket..
        /// </summary>
        /// <param name="socket">The socket to configure.</param>
        /// <param name="enable">Should keep alive messages be sent by this socket.</param>
        /// <param name="time">The time in milliseconds between successful keep alives.</param>
        /// <param name="interval">The time in milliseconds between keep alive retransmissions.</param>
        public static void SetKeepAlive(this Socket socket, bool enable, int time, int interval)
        {
            try
            {
                var keepAliveValues = new byte[sizeof(uint) * 3];

                BitConverter.GetBytes((uint)(enable ? 1 : 0)).CopyTo(keepAliveValues, 0);
                BitConverter.GetBytes((uint)time).CopyTo(keepAliveValues, sizeof(uint));
                BitConverter.GetBytes((uint)interval).CopyTo(keepAliveValues, sizeof(uint) * 2);

                socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, null);
            }
            catch (Exception)
            {
            }
        }
    }
}
