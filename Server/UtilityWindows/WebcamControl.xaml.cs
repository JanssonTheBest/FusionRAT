﻿using Server.UtilityWindows.Interface;
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
    /// Interaction logic for WebcamControl.xaml
    /// </summary>
    public partial class WebcamControl : Window, IUtilityWindow
    {
        public WebcamControl()
        {
            InitializeComponent();
        }
        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
