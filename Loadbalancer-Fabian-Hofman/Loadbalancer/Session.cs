using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Loadbalancer
{
    class Session
    {
        public IPAddress ClientIP { get; set; }
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }

        public Session(IPAddress clientIp, string serverIp, int serverPort)
        {
            ClientIP = clientIp;
            ServerIP = serverIp;
            ServerPort = serverPort;
        }
    }
}
