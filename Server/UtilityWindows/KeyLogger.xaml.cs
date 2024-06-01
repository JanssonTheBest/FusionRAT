using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class KeyLogger :Window, IUtilityWindow
    {
        public KeyLogger()
        {
            InitializeComponent();
        }


        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
