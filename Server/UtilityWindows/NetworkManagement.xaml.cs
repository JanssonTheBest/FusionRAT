﻿using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.Windows;

namespace Server.UtilityWindows
{
    public partial class NetworkManagement : Window, IUtilityWindow
    {
        private readonly ServerSession _serverSession;

        public NetworkManagement(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
        }
    }
}
