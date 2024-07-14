using Server.CoreServerFunctionality;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class SystemOptions : Window
    {
        private readonly ServerSession _serverSession;

        public SystemOptions(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
