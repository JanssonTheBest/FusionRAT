using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class SystemInfo : Window, IUtilityWindow
    {
        public SystemInfo()
        {
            InitializeComponent();
            DataContext = new SystemInfoViewModel();
        }
    }
}
