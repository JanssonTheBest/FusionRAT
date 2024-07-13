using Server.UtilityWindows.Interface;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using Server.CoreServerFunctionality;

namespace Server.UtilityWindows
{
    public partial class FileManager : Window, IUtilityWindow 
    {
        private readonly ServerSession _serverSession;
        private const int MaxItems = 5;

        public FileManager(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
            LoadFolders();
            LoadDrives();
            LoadRecentFiles();
        }

        private void LoadFolders()
        {
            var folders = new List<Folder>
            {
                new() { ImageSource = @"\UI\Assets\Windows\FileManager\FileManagerDefaultFolderIcon.png", FolderTitle = "Desktop", FileCount = GetFileCount("Desktop") },
                new() { ImageSource = @"\UI\Assets\Windows\FileManager\FileManagerDownloadsFolderIcon.png", FolderTitle = "Downloads", FileCount = GetFileCount("Downloads") },
                new() { ImageSource = @"\UI\Assets\Windows\FileManager\FileManagerDocumentsFolderIcon.png", FolderTitle = "Documents", FileCount = GetFileCount("Documents") },
                new() { ImageSource = @"\UI\Assets\Windows\FileManager\FileManagerPicturesFolderIcon.png", FolderTitle = "Pictures", FileCount = GetFileCount("Pictures") },
                new() { ImageSource = @"\UI\Assets\Windows\FileManager\FileManagerVideosFolderIcon.png", FolderTitle = "Videos", FileCount = GetFileCount("Videos") },
            };

            for (int i = 0; i < folders.Count; i++)
            {
                FolderContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
                FolderContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var folder = folders[i];
                var contentControl = new ContentControl
                {
                    ContentTemplate = (DataTemplate)Resources["FolderTemplate"],
                    Content = folder
                };

                Grid.SetColumn(contentControl, i * 2 + 1);
                FolderContainer.Children.Add(contentControl);
            }
        }

        private void LoadDrives()
        {
            var drives = new List<Drive>
            {
                new() { DriveTitle = "Local Disk (C:)", FileSystem = "File System:", Hardware = "Hardware:" },
                new() { DriveTitle = "Backup (D:)", FileSystem = "File System:", Hardware = "Hardware:" },
            };

            foreach (var drive in drives)
            {
                var contentControl = new ContentControl
                {
                    ContentTemplate = (DataTemplate)Resources["DriveTemplate"],
                    Content = drive
                };
                DriveContainer.Children.Add(contentControl);
            }
        }

        private void LoadRecentFiles()
        {
            var recentFiles = new List<RecentFile>
            {
                new() { FileTypeImage = "Images/PNGIcon.png", FileName = "example1.png", FileType = "PNG file", FileSize = "18 MB", LastModified = "Last Modified 2023-10-01" },
                new() { FileTypeImage = "Images/WAVIcon.png", FileName = "example2.wav", FileType = "WAV file", FileSize = "1.53 GB", LastModified = "Last Modified 2023-09-15" },
            };

            foreach (var recentFile in recentFiles)
            {
                var contentControl = new ContentControl
                {
                    ContentTemplate = (DataTemplate)Resources["RecentFileTemplate"],
                    Content = recentFile
                };
                RecentFilesContainer.Children.Add(contentControl);
            }
        }

        private string GetFileCount(string folderName)
        {
            // Example: return Directory.GetFiles(folderPath).Length + " files";
            return "10 files"; // Demo
        }
    }

    public class Folder
    {
        public string ImageSource { get; set; }
        public string FolderTitle { get; set; }
        public string FileCount { get; set; }
    }

    public class Drive
    {
        public string DriveTitle { get; set; }
        public string FileSystem { get; set; }
        public string Hardware { get; set; }
    }

    public class RecentFile
    {
        public string FileTypeImage { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileSize { get; set; }
        public string LastModified { get; set; }
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double windowWidth)
            {
                return windowWidth * 0.4;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
