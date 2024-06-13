using System.Collections.ObjectModel;

namespace Server.UtilityWindows
{
    public class SystemInfoViewModel
    {
        public ObservableCollection<SystemInfoItem> SystemInfoItems { get; set; }

        public SystemInfoViewModel()
        {
            SystemInfoItems =
            [
                new() { Key = "BIOS Version", Value = "1.0.0" },
                new() { Key = "System Manufacturer", Value = "Example Manufacturer" },
                new() { Key = "System Model", Value = "Example Model" },
            ];
        }
    }

    public class SystemInfoItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
