using Server.UI.Pages.SettingsPage.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Server.UI.Pages.SettingsPage
{
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
