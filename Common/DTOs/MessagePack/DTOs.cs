using Common.Comunication;
using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.MessagePack
{
    [Union(0, typeof(PingDTO))]
    [Union(1, typeof(ClientInfoDTO))]
    public interface IPacket
    {
        public Task HandlePacket(Session packetHandler);
    }

    [MessagePackObject]
    public class PingDTO : IPacket
    {
        public async Task HandlePacket(Session session)
        {
            session.OnPing.Invoke(this, EventArgs.Empty);
        }
    }

    [MessagePackObject]
    public class ClientInfoDTO : IPacket
    {
        public string Location { get; set; }
        public string IPAddress { get; set; }
        public string Username { get; set; }
        public string OS { get; set; }
        public string Ping { get; set; }
        public string Version { get; set; }
        public string Date { get; set; }

        public async Task HandlePacket(Session session)
        {
            session.OnClientInfo.Invoke(this, EventArgs.Empty);
        }
    }
}
