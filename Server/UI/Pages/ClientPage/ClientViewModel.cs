using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace Server.UI.Pages.ClientPage
{
    public class ClientViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Cllien> clients;

        public ObservableCollection<Cllien> Clients
        {
            get { return clients; }
            set
            {
                clients = value;
                OnPropertyChanged();
            }
        }

        public ClientViewModel()
        {
            Clients = new ObservableCollection<Cllien>();
            for (int i = 0; i < 100; i++)
            {
                Clients.Add(new Cllien
                {
                    Location = $"Location {i}",
                    IPAddress = $"192.168.0.{i}",
                    Username = $"User{i}",
                    OS = "Windows",
                    Ping = $"{i % 1000} ms",
                    Version = "v1.42",
                    Date = new DateTime(2019, 4, 13).ToShortDateString()
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Cllien
    {
        public string Location { get; set; }
        public string IPAddress { get; set; }
        public string Username { get; set; }
        public string OS { get; set; }
        public string Ping { get; set; }
        public string Version { get; set; }
        public string Date { get; set; }
    }
}
