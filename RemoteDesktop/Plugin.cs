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
