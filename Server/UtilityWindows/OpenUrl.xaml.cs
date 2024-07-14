using Server.CoreServerFunctionality;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class OpenUrl : Window
    {
        private readonly ServerSession _serverSession;

        public OpenUrl(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
