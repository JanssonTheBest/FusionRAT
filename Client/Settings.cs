namespace Client
{
    public static class Settings
    {
#if DEBUG
        public static string ip = "127.0.0.1";
        public static int port = 2332;
        public static string version = "debug";
#else
        public static string ip = "127.0.0.1";
        public static int port = 2332;
        public static string version = "debug";
#endif

    }
}
