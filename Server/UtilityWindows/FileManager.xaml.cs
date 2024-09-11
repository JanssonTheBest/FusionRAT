using Server.CoreServerFunctionality;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Server.UtilityWindows
{
    /// <summary>
    /// Interaction logic for FileManager.xaml
    /// </summary>
    public partial class FileManager : Window
    {
        private readonly ServerSession _serverSession;
        public ObservableCollection<YourDataModel> DataItems { get; set; }
        private Storyboard nav_Shrink;
        private Storyboard nav_Expand;

        public FileManager(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
            nav_Shrink = (Storyboard)FindResource("navShrink");
            nav_Expand = (Storyboard)FindResource("navExpand");

            SizeChanged += FileManager_SizeChanged;
            if (Width <= 710)
            {
                nav_Shrink.Begin(this);
            }
            else
            {
                nav_Expand.Begin(this);
            }

            DataItems = new ObservableCollection<YourDataModel>
            {
                new YourDataModel { Name = "name 1", Size = "1 KB", Modified = new DateTime(2024, 8, 13) },
                new YourDataModel { Name = "name 2", Size = "2 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 3", Size = "3 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 4", Size = "4 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 5", Size = "5 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 6", Size = "6 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 7", Size = "7 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) },
                new YourDataModel { Name = "name 8", Size = "8 KB", Modified = new DateTime(2024, 9, 5) }
            };

            dataGridPorts.ItemsSource = DataItems;
        }

        private void FileManager_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Width <= 710)
            {
                nav_Shrink.Begin(this);
            }
            else
            {
                nav_Expand.Begin(this);
            }
        }
        public class YourDataModel
        {
            public string Name { get; set; }
            public string Size { get; set; }
            public DateTime Modified { get; set; }

            // Combined property for displaying text in the DataGrid
            public string DisplayText => $"Type {Name} Size {Size} Modified {Modified:dd MMM yyyy}";
        }
    }
}
