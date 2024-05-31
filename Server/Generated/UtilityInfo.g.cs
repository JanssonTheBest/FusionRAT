using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Generated
{
    public static partial class UtilityInfo
    {
        public static string[] Utilitites { get; set; }
        public static string[] ImageSources { get; set; }

        static partial void GetUtilityNamesArray();
        static partial void GetImageSourcesArray();
        public static void FetchData()
        {
            GetUtilityNamesArray();
            GetImageSourcesArray();
        }
    }
}