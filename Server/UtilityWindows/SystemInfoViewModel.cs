using System.Collections.ObjectModel;

namespace Server.UtilityWindows
{
    public class SystemInfoViewModel
    {
        public ObservableCollection<SystemInfoItem> SystemInfoItems { get; set; }

        public SystemInfoViewModel()
        {
            SystemInfoItems = new ObservableCollection<SystemInfoItem>
            {
                new SystemInfoItem { Key = "BIOS Version", Value = "1.0.0" },
                new SystemInfoItem { Key = "System Manufacturer", Value = "Example Manufacturer" },
                new SystemInfoItem { Key = "System Model", Value = "Example Model" }
                // Add more items as needed
            };
        }
    }

    public class SystemInfoItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
