using Common.DTOs.MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Common.DTOs.Web
{
    [JsonSerializable(typeof(IpInfo))]
    public class IpInfo
    {
        [JsonInclude]
        public string ip { get; set; }
        [JsonInclude]
        public string city { get; set; }
        [JsonInclude]
        public string region { get; set; }
        [JsonInclude]
        public string country { get; set; }
        [JsonInclude]
        public string loc { get; set; }
        [JsonInclude]
        public string org { get; set; }
        [JsonInclude]
        public string postal { get; set; }
        [JsonInclude]
        public string timezone { get; set; }
        [JsonInclude]
        public string readme { get; set; }

    }
}
