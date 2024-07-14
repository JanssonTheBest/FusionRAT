using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class WebcamControl : Window, IUtilityWindow
    {
        private readonly ServerSession _serverSession;

        public WebcamControl(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
