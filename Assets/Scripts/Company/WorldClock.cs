using System;
using System.Net;
using System.Net.Sockets;

namespace BattleRoyale.Company
{
    public static class WorldClock
    {
        static readonly string[] Hosts =
        {
            "time.google.com",
            "time.cloudflare.com",
            "time.windows.com",
        };

        public static bool TryQuery(out long unixSeconds)
        {
            for (var i = 0; i < Hosts.Length; i++)
            {
                if (TryHost(Hosts[i], out unixSeconds))
                {
                    return true;
                }
            }

            unixSeconds = 0;
            return false;
        }

        static bool TryHost(string host, out long unixSeconds)
        {
            unixSeconds = 0;
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(host);
            }
            catch (Exception)
            {
                return false;
            }

            for (var i = 0; i < addresses.Length; i++)
            {
                if (TryAddress(addresses[i], out unixSeconds))
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryAddress(IPAddress address, out long unixSeconds)
        {
            unixSeconds = 0;
            var packet = new byte[48];
            packet[0] = 0x1B;
            try
            {
                using (var socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.SendTimeout = 2000;
                    socket.ReceiveTimeout = 2000;
                    socket.Connect(new IPEndPoint(address, 123));
                    if (socket.Send(packet) != packet.Length)
                    {
                        return false;
                    }

                    if (socket.Receive(packet) < 48)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            ulong seconds = ((ulong)packet[40] << 24) |
                ((ulong)packet[41] << 16) |
                ((ulong)packet[42] << 8) |
                packet[43];
            const ulong ntpEpoch = 2208988800UL;
            if (seconds <= ntpEpoch)
            {
                return false;
            }

            unixSeconds = (long)(seconds - ntpEpoch);
            return unixSeconds > 0;
        }
    }
}
