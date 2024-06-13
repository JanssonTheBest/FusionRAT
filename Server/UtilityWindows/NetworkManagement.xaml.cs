using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class NetworkManagement : Window, IUtilityWindow
    {
        public NetworkManagement()
        {
            InitializeComponent();
        }

        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
