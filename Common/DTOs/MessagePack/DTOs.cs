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
}
