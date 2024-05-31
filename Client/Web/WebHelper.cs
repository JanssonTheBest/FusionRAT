using Common.DTOs.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client.Web
{
    internal class WebHelper
    {
        public static async Task<IpInfo> RetrieveIpInfo()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var result = httpClient.GetAsync("https://ipinfo.io/json");
                string content = await result.Result.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IpInfo>(content);
            }
        }
    }
}
