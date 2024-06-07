using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Server.UI.Pages.SettingsPage;

namespace Server.UI.Pages.SettingsPage.SettingsPages
{
    /// <summary>
    /// Interaction logic for Appearance.xaml
    /// </summary>
    public partial class Appearance : UserControl
    {

        public Appearance()
        {
            InitializeComponent();
        }

        private void PopupBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        public enum themeNames
        {
            AppearanceCollapsed,
            SettingsContentVisible,
        }

        private void buttonClick(object sender, RoutedEventArgs e)
        {
            Settings.AppearanceStoryboard.DynamicInvoke(themeNames.AppearanceCollapsed);
            Settings.AppearanceStoryboard.DynamicInvoke(themeNames.SettingsContentVisible);
        }

    }
}
