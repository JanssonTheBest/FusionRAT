using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class ClipboardManager : Window, IUtilityWindow
    {
        public ClipboardManager()
        {
            InitializeComponent();
        }

        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
