using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace STUN.Attributes
{
    public class STUNUtils
    {
        public static byte[] Receive(Socket socket, int timeout)
        {
            if (!socket.Poll(timeout * 1000, SelectMode.SelectRead))
            {
                return null;
            }

            EndPoint endPoint = new IPEndPoint(
                socket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

            byte[] buffer = new byte[1024 * 2];
            int bytesRead = 0;

            bytesRead = socket.ReceiveFrom(buffer, ref endPoint);

            return buffer.Take(bytesRead).ToArray();
        }

        public static bool TryParseHostAndPort(string hostAndPort, out IPEndPoint endPoint)
        {
            return TryParseHostAndPort(hostAndPort, AddressFamily.Unspecified, out endPoint);
        }

        /// <summary>
        /// Resolves an endpoint while selecting an address compatible with the socket that will use it.
        /// This avoids sending an IPv6 DNS result through an IPv4 socket (or vice versa).
        /// </summary>
        public static bool TryParseHostAndPort(string hostAndPort, AddressFamily addressFamily,
            out IPEndPoint endPoint)
        {
            if (string.IsNullOrWhiteSpace(hostAndPort))
            {
                endPoint = null;
                return false;
            }

            string host;
            string portText;
            hostAndPort = hostAndPort.Trim();
            if (hostAndPort[0] == '[')
            {
                var bracket = hostAndPort.IndexOf(']');
                if (bracket < 2 || bracket + 2 >= hostAndPort.Length || hostAndPort[bracket + 1] != ':')
                {
                    endPoint = null;
                    return false;
                }
                host = hostAndPort.Substring(1, bracket - 1);
                portText = hostAndPort.Substring(bracket + 2);
            }
            else
            {
                var separator = hostAndPort.LastIndexOf(':');
                if (separator < 1 || hostAndPort.IndexOf(':') != separator)
                {
                    endPoint = null;
                    return false;
                }
                host = hostAndPort.Substring(0, separator);
                portText = hostAndPort.Substring(separator + 1);
            }

            if (!ushort.TryParse(portText, out ushort port))
            {
                endPoint = null;
                return false;
            }

            if (!IPAddress.TryParse(host, out IPAddress address))
            {
                try
                {
#if NETSTANDARD1_3
                    var addresses = Dns.GetHostEntryAsync(host).GetAwaiter().GetResult().AddressList;
#else
                    var addresses = Dns.GetHostEntry(host).AddressList;
#endif
                    address = addressFamily == AddressFamily.Unspecified
                        ? addresses.FirstOrDefault()
                        : addresses.FirstOrDefault(candidate => candidate.AddressFamily == addressFamily);
                }
                catch
                {
                    endPoint = null;
                    return false;
                }
            }

            if (address == null ||
                (addressFamily != AddressFamily.Unspecified && address.AddressFamily != addressFamily))
            {
                endPoint = null;
                return false;
            }

            endPoint = new IPEndPoint(address, port);
            return true;
        }

        public static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            if (b1 == b2)
                return true;

            if (b1.Length != b2.Length)
                return false;

            return b1.SequenceEqual(b2);
        }
    }
}
