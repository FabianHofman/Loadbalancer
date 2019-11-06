using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Loadbalancer.Loadbalancer
{
    public class Server
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool Alive { get; set; }

        public Server(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public async void AskForHealth()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GET / HTTP/1.1");
            builder.AppendLine($"Host: {Host}");
            builder.AppendLine("Connection: close");
            builder.AppendLine();
            byte[] header = Encoding.ASCII.GetBytes(builder.ToString());

            try
            {
                TcpClient client = new TcpClient(Host, Port);

                using (NetworkStream stream = client.GetStream())
                using (MemoryStream memstream = new MemoryStream())
                {
                    await stream.WriteAsync(header, 0, header.Length);

                    if (stream.DataAvailable)
                    {
                        await stream.CopyToAsync(memstream);
                        string response = Encoding.ASCII.GetString(memstream.GetBuffer());

                        if (response.Contains("200 OK"))
                        {
                            Alive = true;
                        } 
                        else
                        {
                            Alive = false;
                        }
                    }
                }
            }
            catch
            {
                Alive = false;
            }
        }

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }
}
