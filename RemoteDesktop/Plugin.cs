﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common.Comunication;
using Common.DTOs.MessagePack;

namespace RemoteDesktopPlugin
{
    public class Plugin
    {
        Session _session;
        public Plugin(Session session)
        {
            _session = session;
            session.OnRemoteDesktop += HandlePacket;
            var screenStateLogger = new ScreenStateLogger();
            screenStateLogger.ScreenRefreshed += OnNewFrame;
            screenStateLogger.Start();
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            
        }

        private async void OnNewFrame(object? sender, byte[] e)
        {
            await _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Frame = e
            });
        }
    }
}
