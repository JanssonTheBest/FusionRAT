using System.Windows.Media.Imaging;
using Common.DTOs.MessagePack;
using Common.Communication;
using System.Windows;
using System.IO;

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
        public BitmapImage Flag { get; set; }
        public ServerSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
            AssignClientInfo(new ClientInfoDTO()
            {
                Location = "loading...",
                Date = DateTime.Now.ToString(),
                IPAddress = "loading...",
                OS = "loading...",
                Ping = "loading...",
                Username = "loading...",
                Version = "loading...",
            });

            OnPing += HandlePing;
            OnClientInfo += HandleClientInfo;

            SendPlugin(typeof(ClientInfo.Plugin));
            HandlePing(null, EventArgs.Empty);
        }

        public async Task SendPlugin(Type plugin)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), plugin.Namespace + ".dll");
            byte[] bytes = await File.ReadAllBytesAsync(path);
            await SendPacketAsync(new PluginDTO()
            {
                Plugin = bytes,
                PluginFullName = plugin.FullName,
            });
        }

        private void HandleClientInfo(object? sender, EventArgs e)
        {
            var info = (ClientInfoDTO)sender;
            info.Date = DateTime.Now.ToString();
            AssignClientInfo(info);
        }

        DateTime oldTime = DateTime.Now;
        private async void HandlePing(object? sender, EventArgs e)
        {
            Task.Run(async() =>
            {
                var dateTime = DateTime.Now;
                Ping = Convert.ToString((dateTime - oldTime).Milliseconds);
                oldTime = dateTime;
                await ClientHandler.UpdateClients(this);
                await Task.Delay(2000);
                await SendPacketAsync(new PingDTO());
            });
        }

        private void AssignClientInfo(ClientInfoDTO clientInfoDTO)
        {
            Location = clientInfoDTO.Country;
            IPAddress = clientInfoDTO.IPAddress;
            Username = clientInfoDTO.Username;
            OS = clientInfoDTO.OS;
            Ping = clientInfoDTO.Ping;
            Version = clientInfoDTO.Version;
            Date = clientInfoDTO.Date;
            Application.Current.Dispatcher.Invoke(() => Flag = new BitmapImage(new Uri($"https://flagsapi.com/{clientInfoDTO.Country}/flat/64.png")));
            ClientHandler.UpdateClients(this).GetAwaiter().GetResult();
        }
        List<Window> windows = [];
        public void OpenUtilityWindow(Window utilityWindow)
        {
            utilityWindow.Closed += ((sender, e) =>
            {
                windows.Remove(utilityWindow);
            });

            utilityWindow.Show();

            windows.Add(utilityWindow);
        }
    }
}
