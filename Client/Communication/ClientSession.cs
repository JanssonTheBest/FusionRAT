using Common.Comunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Communication
{
    internal class ClientSession : Session
    {
        public ClientSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
        }
    }
}
