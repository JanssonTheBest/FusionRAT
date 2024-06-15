using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Server.UI.Pages.SettingsPage.Pages
{
    public partial class Appearance : UserControl
    {
        public ObservableCollection<ThemeColors> Themes { get; set; }

        public Appearance()
        {
            InitializeComponent();
            DataContext = this;
            Themes =
            [
                new() { Primary = "#202020", Secondary = "#272727", Border = "#4B4B4B", Accent = "#FF4081", Text = "#FFFFFF", Selected = "#E91E63" },
                new() { Primary = "#131313", Secondary = "#000000", Border = "#4B4B4B", Accent = "#FF4081", Text = "#FFFFFF", Selected = "#E91E63" },
                new() { Primary = "#16141C", Secondary = "#2B293D", Border = "#4B4B4B", Accent = "#FF4081", Text = "#FFFFFF", Selected = "#E91E63" },
                new() { Primary = "#372E45", Secondary = "#453C56", Border = "#4B4B4B", Accent = "#FF4081", Text = "#FFFFFF", Selected = "#E91E63" },
                new() { Primary = "#FFFFFF", Secondary = "#F5F5F5", Border = "#4B4B4B", Accent = "#FF4081", Text = "#000000", Selected = "#E91E63" },
                new() { Primary = "#535364", Secondary = "#F5F5F5", Border = "#4B4B4B", Accent = "#FF4081", Text = "#000000", Selected = "#E91E63" },
                new() { Primary = "#282F54", Secondary = "#F5F5F5", Border = "#4B4B4B", Accent = "#FF4081", Text = "#000000", Selected = "#E91E63" },
                new() { Primary = "#44348C", Secondary = "#F5F5F5", Border = "#4B4B4B", Accent = "#FF4081", Text = "#000000", Selected = "#E91E63" }
            ];
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is ThemeColors themeColors)
            {
                ChangeTheme(themeColors);
            }
        }

        private void ChangeTheme(ThemeColors colors)
        {
            Application.Current.Resources["Primary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Primary));
            Application.Current.Resources["Secondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Secondary));
            Application.Current.Resources["Border"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Border));
            Application.Current.Resources["Accent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Accent));
            Application.Current.Resources["Text"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Text));
            Application.Current.Resources["Selected"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.Selected));
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

    public class ThemeColors
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Border { get; set; }
        public string Accent { get; set; }
        public string Text { get; set; }
        public string Selected { get; set; }
    }
}
