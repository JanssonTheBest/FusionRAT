using Server.CoreServerFunctionality;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;

namespace Server.UI.Pages.ServerPage
{
    public partial class Server : UserControl
    {
        public ObservableCollection<PortStatus> Ports { get; set; } = new ObservableCollection<PortStatus>();

        public Server()
        {
            InitializeComponent();
            listener.OnClientConnected += OnClientConnected;
            dataGridPorts.ItemsSource = Ports;
        }

        Listener listener = new();
        private void portStart_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(portInput.Text);
            listener.AddPortToListener(port).GetAwaiter().GetResult();
            Ports.Add(new PortStatus { Port = port, Status = "Listening", Name = "Placeholder" });
        }

        private void OnClientConnected(object sender, EventArgs e)
        {
            Task.Run(async() =>
            {
                await ClientHandler.HandleClient((TcpClient)sender);
            });
        }
    }

    public class PortStatus
    {
        public int Port { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
    }
}
