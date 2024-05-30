using Common.DTOs.MessagePack;
using Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Server.Helper
{
    internal static class ApplicationHelperMethods
    {
        public static async Task<List<string>> RetrievUtilityNames()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(a => a.Namespace == "Server.UtilityWindows" && a.Name.EndsWith(".cs")).Select(a => a.Name.Substring(a.Name.IndexOf('.'))).ToList();        
        }

        public static async Task<BitmapImage> RetrievIconThroughUtilityName(string utility)
        {
            return (BitmapImage)Assembly.GetExecutingAssembly().GetType(utility).GetProperty(nameof(IUtilityWindow.UtilityIcon)).GetValue(null);
        }

        public static async Task<BitmapImage> RetrieveServerSessionByClientInfo(ClientInfoDTO info)
        {
            return (BitmapImage)Assembly.GetExecutingAssembly().GetType(utility).GetProperty(nameof(IUtilityWindow.UtilityIcon)).GetValue(null);
        }
    }
}
