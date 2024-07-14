using Server.CoreServerFunctionality;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class ClientChat : Window
    {
        private readonly ServerSession _serverSession;

        public ClientChat(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
