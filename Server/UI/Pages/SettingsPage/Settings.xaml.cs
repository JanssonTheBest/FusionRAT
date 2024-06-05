using Server.UI.Pages.SettingsPage.SettingsPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server.UI.Pages.SettingsPage
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            AppearanceStoryboard = AppearanceStoryboardStart;
        }

        public void AppearanceStoryboardStart(Appearance.themeNames papi)
        {

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ((Storyboard)FindResource(papi.ToString())).Begin();
            }));
        }

        public static Delegate AppearanceStoryboard;

    }
}
