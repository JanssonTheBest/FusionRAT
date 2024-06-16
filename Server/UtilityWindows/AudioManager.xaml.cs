using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class AudioManager : Window, IUtilityWindow
    {
        ServerSession _session;
        public AudioManager(ServerSession session)
        {
            InitializeComponent();
            _session = session;
        }
    }
}
