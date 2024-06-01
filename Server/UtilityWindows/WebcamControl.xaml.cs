using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class WebcamControl : Window, IUtilityWindow
    {
        public WebcamControl()
        {
            InitializeComponent();
        }
        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
