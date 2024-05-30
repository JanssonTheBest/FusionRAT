using Common.Comunication;
using Common.DTOs.MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.CoreServerFunctionality
{
    public class ServerSession : Session
    {
        public ClientInfoDTO clientInfo;
        public ServerSession(SslStream sslStream, TcpClient client) : base(sslStream,client)
        {
            
        }
    }
}
