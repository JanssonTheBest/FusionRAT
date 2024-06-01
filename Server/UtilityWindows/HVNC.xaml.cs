using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class HVNC :Window, IUtilityWindow
    {
        public HVNC()
        {
            InitializeComponent();
        }


        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
