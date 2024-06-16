using Server.CoreServerFunctionality;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace Server.UI.Pages.ServerPage
{
    public partial class Server : UserControl
    {
        public Server()
        {
            InitializeComponent();
            listener.OnClientConnected += OnClientConnected;
        }
        Listener listener = new();
        private void portStart_Click(object sender, RoutedEventArgs e)
        {
            listener.AddPortToListener(int.Parse(portInput.Text)).GetAwaiter().GetResult();
        }

        private void OnClientConnected(object sender, EventArgs e)
        {
            Task.Run(async() =>
            {
                await ClientHandler.HandleClient((TcpClient)sender);
            });
        }
    }
}
