using Client.Communication;
using Client.Recording;
using Common.DTOs.MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Utilities
{
    internal class RemoteDesktop
    {
        ClientSession _session;
        public RemoteDesktop(ClientSession session)
        {
            _session = session;
            session.OnRemoteDesktop += HandlePacket;
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            if (!screenrecorderTask.IsCompleted)
            {
                return;
            }

            screenrecorderTask = Task.Run(() =>
            {
                var screenStateLogger = new ScreenStateLogger();
                screenStateLogger.ScreenRefreshed += OnNewFrame;
                screenStateLogger.Start();
            });
        }

        private async void OnNewFrame(object? sender, byte[] e)
        {
            await _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Frame = e
            });
        }

        Task screenrecorderTask=Task.CompletedTask;
        
        private async Task RecorderLoop()
        {
           

        }
    }
}
