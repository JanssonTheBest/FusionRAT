using Server.CoreServerFunctionality;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class Keylogger : Window
    {
        private readonly ServerSession _serverSession;

        public Keylogger(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
