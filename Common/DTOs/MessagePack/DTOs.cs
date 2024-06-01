using Common.Comunication;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.MessagePack
{
    [Union(0, typeof(PingDTO))]
    [Union(1, typeof(ClientInfoDTO))]
    [Union(2, typeof(RemoteDesktopDTO))]
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
    public class RemoteDesktopDTO : IPacket
    {
        [Key(0)]
        public byte[] Frame { get; set; }
        public async Task HandlePacket(Session session)
        {
            session.OnRemoteDesktop.Invoke(this, EventArgs.Empty);
        }
    }

    [MessagePackObject]
    public class ClientInfoDTO : IPacket
    {
        [Key(0)]
        public string Location { get; set; }
        [Key(1)]
        public string IPAddress { get; set; }
        [Key(2)]
        public string Username { get; set; }
        [Key(3)]
        public string OS { get; set; }
        [Key(4)]
        public string Ping { get; set; }
        [Key(5)]
        public string Version { get; set; }
        [Key(6)]
        public string Date { get; set; }
        [Key(7)]
        public string Country {  get; set; }

        public async Task HandlePacket(Session session)
        {
            session.OnClientInfo.Invoke(this, EventArgs.Empty);
        }
    }
}
