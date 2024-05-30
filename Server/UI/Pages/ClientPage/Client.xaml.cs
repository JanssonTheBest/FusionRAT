using Common.DTOs.MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

namespace Server.UI.Pages.ClientPage
{
    /// <summary>
    /// Interaction logic for Client.xaml
    /// </summary>
    public partial class Client : UserControl
    {
        private ObservableCollection<ClientInfoDTO> clients = new();
        private SemaphoreSlim semaphoreSlim = new(1, 1);
        public Client()
        {
            InitializeComponent();
            clients.CollectionChanged += UpdateClientGrid;
        }

        private async Task RemoveClient(ClientInfoDTO clientInfoDTO)
        {
            await semaphoreSlim.WaitAsync();
            clients.Remove(clientInfoDTO);
            semaphoreSlim.Release();
        }

        private async Task AddClient(ClientInfoDTO clientInfoDTO)
        {
            await semaphoreSlim.WaitAsync();
            clients.Add(clientInfoDTO);
            semaphoreSlim.Release();
        }

        private void UpdateClientGrid(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    dataGrid.Items.Remove(item);
                }
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    dataGrid.Items.Add(item);
                }
                return;
            }
        }
    }
}
