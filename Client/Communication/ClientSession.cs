using Common.Comunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Web;
using Common.DTOs.Web;
using Common.DTOs.MessagePack;
using System.Runtime.InteropServices;

namespace Client.Communication
{
    internal class ClientSession : Session
    {
        ClientInfoDTO clientInfo;
        public ClientSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
            Task.Run(SendClientInfo);
            OnPing += HandlePing;
        }

        private async void HandlePing(object? sender, EventArgs e)
        {
            await SendPacketAsync(new PingDTO());
        }

        private async Task SendClientInfo()
        {
            var info = await WebHelper.RetrieveIpInfo();
            clientInfo = new ClientInfoDTO()
            {
                IPAddress = info.ip,
                Date = "...",
                OS = RuntimeInformation.OSDescription,
                Location = info.loc,
                Version = Settings.version,
                Username = Environment.UserName,
                Ping = "...",
                Country=info.country,
            };
            await SendPacketAsync(clientInfo);
        }
    }
}
