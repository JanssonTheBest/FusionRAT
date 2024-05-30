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
        public static string ip = "127.0.0.1";
        public static int port = 2332;
#else

#endif

    }
}
