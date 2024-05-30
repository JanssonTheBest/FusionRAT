using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.ConnectionPhase
{
    internal static class ConnectionHelper
    {
        public static void Connect()
        {
            var client = new TcpClient(Settings.ip,Settings.port);
        }
    }
}
