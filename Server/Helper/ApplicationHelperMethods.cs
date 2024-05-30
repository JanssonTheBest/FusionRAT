using Common.DTOs.MessagePack;
using Server.UtilityWindows.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Server.Helper
{
    internal static class ApplicationHelperMethods
    {
        public static async Task<List<string>> RetrievUtilityNames()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(a => a.Namespace == "Server.UtilityWindows" && a.Name.EndsWith(".cs")).Select(a => a.Name.Substring(a.Name.IndexOf('.'))).ToList();        
        }

        public static async Task<Image> RetrievIconThroughUtilityName(string utility)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (string resourceName in resourceNames)
            {
                if (resourceName.EndsWith(".xaml") && resourceName.Contains(utility))
                {
                    Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
                    if (resourceStream != null)
                    {
                        using (StreamReader reader = new StreamReader(resourceStream))
                        {
                            string xamlString = reader.ReadToEnd();

                            StringReader stringReader = new StringReader(xamlString);
                            XmlReader xmlReader = XmlReader.Create(stringReader);

                            object xamlObject = XamlReader.Load(xmlReader);

                            if (xamlObject is FrameworkElement frameworkElement)
                            {
                                Image image = frameworkElement.FindName("icon") as Image;
                                if (image != null)
                                {
                                    return image;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static async Task<BitmapImage> RetrieveServerSessionByClientInfo(ClientInfoDTO info)
        {
            return null;
        }
    }
}
