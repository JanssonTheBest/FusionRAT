using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public static class Settings
    {
#if DEBUG
        public static string ip = "84.216.179.171";
        public static int port = 2332;
        public static string version = "debug";
#else

#endif

    }
}
