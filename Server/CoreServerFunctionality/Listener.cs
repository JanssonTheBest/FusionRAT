using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.CoreServerFunctionality
{
    internal class Listener : IDisposable
    {

        public EventHandler<EventArgs> OnClientConnected;

        SemaphoreSlim semaphore = new(1, 1);
        List<ListenerJob> listeners = new();
        public async Task AddPortToListener(int port)
        {
            await semaphore.WaitAsync();

            if (listeners.Where(a => a._port == port).FirstOrDefault() != default)
            {
                semaphore.Release();
                return;
            }
            var listener = new ListenerJob(port, this);
            listener.Start();
            listeners.Add(listener);
        }

        public async Task RemovePortFromListener(int port)
        {
            await semaphore.WaitAsync();
            listeners.RemoveAll(a => a._port == port);
            semaphore.Release();
        }

        public async void Dispose()
        {
            await semaphore.WaitAsync();
            listeners.ForEach(a => a.Dispose());
            semaphore.Release();
        }

        private class ListenerJob : IDisposable
        {
            public int _port;
            private CancellationTokenSource cts = new CancellationTokenSource();
            private Task start = Task.CompletedTask;
            private Listener _listener;
            public ListenerJob(int port, Listener listener)
            {
                _port = port;
                _listener = listener;
            }
            public void Start()
            {
                TcpListener listener = new(IPAddress.Any, _port);
                Task.Run(async () =>
                {
                    listener.Start();
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var client = await listener.AcceptTcpClientAsync();
                        _listener.OnClientConnected.Invoke(client, EventArgs.Empty);
                    }
                    listener.Dispose();
                }, cts.Token);
            }
            public void Dispose()
            {
                cts.Cancel();
                start.Wait();
            }
        }
    }


}
