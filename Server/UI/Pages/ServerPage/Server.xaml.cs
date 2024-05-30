using Server.CoreServerFunctionality;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server.UI.Pages.ServerPage
{
    /// <summary>
    /// Interaction logic for Server.xaml
    /// </summary>
    public partial class Server : UserControl
    {
        public Server()
        {
            InitializeComponent();
        }
        Listener listener = new Listener();
        private void portStart_Click(object sender, RoutedEventArgs e)
        {
            listener.AddPortToListener(int.Parse(portInput.Text)).GetAwaiter().GetResult();
        }
    }
}
