using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class RegistryManager : Window, IUtilityWindow
    {
        private readonly ServerSession _serverSession;

        public RegistryManager(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
