using Client.Communication;
using Common.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Client.ConnectionPhase
{
    internal static class ConnectionHelper
    {
        public static void Connect()
        {
            var session = new ClientSession(new AuthenticatedServer());
        }
    }

    public class AuthenticatedServer : IConnectionProperties
    {
        public TcpClient Client { get; set; }
        public SslStream SslStream { get; set; }
        public AuthenticatedServer()
        {
            AuthenticateServer();
        }

        private void AuthenticateServer()
        {
            try
            {
                Client = new TcpClient(Settings.ip, Settings.port);
                var networkStream = Client.GetStream();
                SslStream = new SslStream(networkStream);
                var options = new SslClientAuthenticationOptions()
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                };
                options.RemoteCertificateValidationCallback += OnRemoteCertificateValidationCallback;
                SslStream.AuthenticateAsClient(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Task.Delay(300).Wait();
                AuthenticateServer();
            }
        }

        private bool OnRemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
