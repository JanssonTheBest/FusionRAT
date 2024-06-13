using Common.Communication;
using Common.DTOs.MessagePack;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Client.Communication
{
    internal class ClientSession : Session
    {
        ClientInfoDTO clientInfo;
        List<object> activePlugins = new List<object>();
        public ClientSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
            OnPing += HandlePing;
            OnPlugin += HandlePlugin;
        }

        private void HandlePlugin(object? sender, EventArgs e)
        {
            var DTO = (PluginDTO)sender;
            var assembly = Assembly.Load(DTO.Plugin);
            var plugin = Activator.CreateInstance(assembly.GetType(DTO.PluginFullName), this);
            activePlugins.Add(plugin);

        }

        private async void HandlePing(object? sender, EventArgs e)
        {
            await SendPacketAsync(new PingDTO());
        }
    }
}
