using Server.CoreServerFunctionality;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class ScheduledTasksManager : Window
    {
        private readonly ServerSession _serverSession;

        public ScheduledTasksManager(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
