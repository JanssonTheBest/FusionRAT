﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Server.UI.Pages.SettingsPage.Pages
{
    public partial class Appearance : UserControl
    {
        private readonly List<Theme> _themes =
        [
            new() { Primary = "#202020", Secondary = "#272727", Border = "#4B4B4B", Text = "#FFFFFF", Selected = "#E91E63", Icon = "#FFFFFF" },
            new() { Primary = "#0B0B0B", Secondary = "#0F0F0F", Border = "#1B1B1B", Text = "#FFFFFF", Selected = "#E91E63", Icon = "#FFFFFF" },
            new() { Primary = "#16141C", Secondary = "#1A1820", Border = "#4C495B", Text = "#FFFFFF", Selected = "#E91E63", Icon = "#FFFFFF" },
            new() { Primary = "#011623", Secondary = "#041A2D", Border = "#0E2B44", Text = "#FFFFFF", Selected = "#E91E63", Icon = "#FFFFFF" },
            new() { Primary = "#FFFFFF", Secondary = "#F5F5F5", Border = "#4B4B4B", Text = "#000000", Selected = "#E91E63", Icon = "#000000" },
            new() { Primary = "#535364", Secondary = "#F5F5F5", Border = "#4B4B4B", Text = "#000000", Selected = "#E91E63", Icon = "#000000" },
            new() { Primary = "#282F54", Secondary = "#F5F5F5", Border = "#4B4B4B", Text = "#000000", Selected = "#E91E63", Icon = "#000000" },
            new() { Primary = "#44348C", Secondary = "#F5F5F5", Border = "#4B4B4B", Text = "#000000", Selected = "#E91E63", Icon = "#000000" }
        ];

        private readonly List<string> _shades =
        [
            "#1E9BFD", "#FF0000", "#B7A6FF", "#936DFF", "#FFEC3D", "#6DFF7C", "#FFFFFF"
        ];

        public Appearance()
        {
            InitializeComponent();
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                int themeIndex = int.Parse(radioButton.Name.Replace("ThemeRadioButton", "")) - 1;
                ApplyTheme(_themes[themeIndex]);
            }
        }

        private void ApplyTheme(Theme theme)
        {
            Application.Current.Resources["Primary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Primary));
            Application.Current.Resources["Secondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Secondary));
            Application.Current.Resources["Border"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Border));
            Application.Current.Resources["Text"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Text));
            Application.Current.Resources["Selected"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Selected));
            Application.Current.Resources["Icon"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Icon));
        }

        private void ShadeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                string shade = radioButton.Background.ToString();
                ApplyAccent(shade);
            }
        }

        private void ApplyAccent(string shade)
        {
            Application.Current.Resources["Accent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(shade));
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

    public class Theme
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Border { get; set; }
        public string Text { get; set; }
        public string Selected { get; set; }
        public string Icon { get; set; }
    }
}
