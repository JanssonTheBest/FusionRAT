using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class Notes : Window, IUtilityWindow
    {
        public Notes()
        {
            InitializeComponent();
            var items = new List<Item>
            {
                new() { Name = "Passwords", LastModified = "2024-01-03 14:27" },
                new() { Name = "Work", LastModified = "2023-06-30 03:55" },
                new() { Name = "General", LastModified = "2022-12-07 11:50" }
            };

            listView.ItemsSource = items;
        }

        #region Click Events
        private void ImportBTN_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ExportBTN_Click(object sender, RoutedEventArgs e)
        {

        }
        private void NewBTN_Click(object sender, RoutedEventArgs e)
        {

        }
        private void DeleteBTN_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SaveBTN_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion
    }

    public class Item
    {
        public string Name { get; set; }
        public string LastModified { get; set; }
    }
}
