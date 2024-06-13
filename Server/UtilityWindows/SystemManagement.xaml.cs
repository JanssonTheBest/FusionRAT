using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class SystemManagement : Window, IUtilityWindow
    {
        public SystemManagement()
        {
            InitializeComponent();
        }

        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
