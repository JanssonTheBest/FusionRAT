﻿using Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Server.UtilityWindows
{
    /// <summary>
    /// Interaction logic for RegistryManager.xaml
    /// </summary>
    public partial class RegistryManager : Window, IUtilityWindow
    {
        public RegistryManager()
        {
            InitializeComponent();
        }

        public BitmapImage UtilityIcon => throw new NotImplementedException();
    }
}
