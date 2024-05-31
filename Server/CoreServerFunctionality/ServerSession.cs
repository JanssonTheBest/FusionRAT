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
        public string Location { get; set; }
        public string IPAddress { get; set; }
        public string Username { get; set; }
        public string OS { get; set; }
        public string Ping { get; set; }
        public string Version { get; set; }
        public string Date { get; set; }
        public ServerSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
            AssignClientInfo(new ClientInfoDTO()
            {
                Location = "loading...",
                Date = DateTime.Now.ToString(),
                IPAddress= "loading...",
                OS="loading...",
                Ping="loading...",
                Username="Client"+Guid.NewGuid().ToString(),
                Version="loading...", 
            });
        }

        private void AssignClientInfo(ClientInfoDTO clientInfoDTO)
        {
            Location = clientInfoDTO.Location;
            IPAddress = clientInfoDTO.IPAddress;
            Username = clientInfoDTO.Username;
            OS = clientInfoDTO.OS;  
            Ping = clientInfoDTO.Ping;  
            Version = clientInfoDTO.Version;    
            Date = clientInfoDTO.Date;
            ClientHandler.UpdateClients(this).GetAwaiter().GetResult();
        }
    }
}
