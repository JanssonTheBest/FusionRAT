using Common.Comunication;
using Common.DTOs.MessagePack;
using MessagePack.Formatters;
using Server.UtilityWindows.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

            HandlePing(null, EventArgs.Empty);
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
            var dateTime = DateTime.Now;
            Ping = Convert.ToString((dateTime - oldTime).Milliseconds);
            oldTime = dateTime;
            await ClientHandler.UpdateClients(this);
            await Task.Delay(2000);
            await SendPacketAsync(new PingDTO());
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
            Application.Current.Dispatcher.Invoke(()=> Flag = new BitmapImage(new Uri($"https://flagsapi.com/{clientInfoDTO.Country}/flat/64.png")));
            ClientHandler.UpdateClients(this).GetAwaiter().GetResult();
        }
        List<Window> windows = new List<Window>();
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
