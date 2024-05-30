using Server.CoreServerFunctionality;
using Server.UI.Pages.ClientPage;
using Server.UtilityWindows.Interface;
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
    /// Interaction logic for AudioManager.xaml
    /// </summary>
    public partial class AudioManager :Window, IUtilityWindow
    {
        ServerSession _session;
        public AudioManager(ServerSession session)
        {
            InitializeComponent();
            _session = session;
        }


        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
