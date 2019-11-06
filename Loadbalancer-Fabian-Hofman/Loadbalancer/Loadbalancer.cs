using Loadbalancer.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Loadbalancer.Loadbalancer
{
    class Loadbalancer
    {
        #region UI-items
        public ObservableCollection<ListBoxItem> Log { get; set; }
        public ObservableCollection<ListBoxItem> Algorithms { get; set; }
        public ObservableCollection<ListBoxItem> ServerList { get; set; }
        #endregion

        #region Selected-UI-items
        public ListBoxItem SelectedServer { get; set; }
        public string SelectedAlgorithmString;
        #endregion

        #region Algorithms
        private IAlgorithmFactory AlgoFactory;
        private IAlgorithm CurrentAlgorithm;
        private string PreviousAlgorithmString;
        #endregion

        #region Settings
        public bool IsRunning { get; set; }
        public int Port { get; set; }
        public int HealthCheckInterval { get; set; }
        public int BufferSize { get; set; }
        public string AddServerIP { get; set; }
        public int AddServerPort { get; set; }
        #endregion

        #region Sessions
        private List<Session> Sessions;
        #endregion

        private IPAddress _ipAddress;
        private TcpListener _tcpListener;
        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private List<Server> Servers;

        public Loadbalancer()
        {
            IsRunning = false;
            _ipAddress = IPAddress.Parse("127.0.0.1");
            Port = 8080;
            HealthCheckInterval = 15000;
            BufferSize = 1024;
            AddServerIP = "127.0.0.1";
            AddServerPort = 8081;
            ServerList = new ObservableCollection<ListBoxItem>();
            Servers = new List<Server>();
            Log = new ObservableCollection<ListBoxItem>();
            Algorithms = new ObservableCollection<ListBoxItem>();
            AlgoFactory = new IAlgorithmFactory();
            CurrentAlgorithm = null;

            Sessions = new List<Session>();

            InitAlgos();
            DoHealthCheck();
        }

        public void Start()
        {
            if (ValidateLoadbalancerPreferences(Port, BufferSize))
            {
                try
                {
                    _tcpListener = new TcpListener(_ipAddress, Port);
                    _tcpListener.Start();
                    IsRunning = true;
                    Task.Run(() => ListenForIncomingClients());
                    AddToLog("Started Loadbalancer!");
                }
                catch
                {
                    IsRunning = false;
                    AddToLog("Could not start the Loadbalancer!");
                }
            }
        }

        public void Stop()
        {
            while (true)
            {
                if (_tcpListener.Pending()) continue;

                _tcpListener.Stop();
                IsRunning = false;
                AddToLog("Stopped Loadbalancer!");
                break;
            }
        }

        private void ListenForIncomingClients()
        {
            AddToLog("Waiting for new requests..");

            while (IsRunning)
            {
                try
                {
                    TcpClient incomingClient = _tcpListener.AcceptTcpClient();
                    Task.Run(() => HandleClient(incomingClient));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            }
        }

        private async Task HandleClient(TcpClient incomingClient)
        {
            using (incomingClient)
            using (NetworkStream incomingClientStream = incomingClient.GetStream())
            {
                HttpRequest httpRequest = await ReceiveHttpRequest(incomingClientStream);
                HttpResponse httpResponse = null;

                if (httpRequest != null)
                {
                    AddToLog($"\r\n----------Request Received----------\r\n{httpRequest.ToString}\r\n");

                    if (httpResponse == null)
                    {
                        httpResponse = await StreamHttpResponseFromServerToClient(httpRequest, incomingClient);
                    }

                    if (httpResponse != null)
                    {
                        AddToLog($"\r\n----------Response Sent {httpResponse.GetHeader("Host")}----------\r\n{httpResponse.ToString}\r\n");
                    }
                }
            }
        }

        private async Task<HttpRequest> ReceiveHttpRequest(NetworkStream incomingClientStream)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    byte[] requestBuffer = new byte[BufferSize];
                    int bytesReceived;

                    if (incomingClientStream.CanRead)
                    {
                        if (incomingClientStream.DataAvailable)
                        {
                            do
                            {
                                bytesReceived = await incomingClientStream.ReadAsync(requestBuffer, 0, requestBuffer.Length);
                                await memoryStream.WriteAsync(requestBuffer, 0, bytesReceived);
                            } while (incomingClientStream.DataAvailable);
                        }
                    }

                    byte[] requestBytes = memoryStream.ToArray();
                    HttpRequest httpRequest = HttpRequest.TryParse(requestBytes);

                    return httpRequest;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return null;
            }
        }

        private async Task<HttpResponse> StreamHttpResponseFromServerToClient(HttpRequest httpRequest, TcpClient incomingClient)
        {

            Server selectedServer = null;

            if (httpRequest.HasHeader("Cookie") && SelectedAlgorithmString == "CookieBased")
            {
                AddToLog("Using Cookie Persistence for next request..");
                selectedServer = GetServerForCookie(httpRequest);
            }


            if (SelectedAlgorithmString == "SessionBased")
            {
                AddToLog("Using Session Persistence for next request..");
                selectedServer = GetServerForSession(incomingClient);
            }

            if (selectedServer == null)
            {
                selectedServer = DetermineServer();
            }

            using (TcpClient clientToApproach = new TcpClient(selectedServer.Host, selectedServer.Port))
            using (NetworkStream clientToApproachStream = clientToApproach.GetStream())
            {
                NetworkStream incomingClientStream = incomingClient.GetStream();
                await clientToApproachStream.WriteAsync(httpRequest.ToBytes, 0, httpRequest.ToBytes.Length);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    try
                    {
                        byte[] responseBuffer = new byte[BufferSize];

                        while (true)
                        {
                            int bytesReceived = await clientToApproachStream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                            await memoryStream.WriteAsync(responseBuffer, 0, bytesReceived);
                            if (!clientToApproachStream.DataAvailable) break;
                        }

                        byte[] responseBytes = memoryStream.ToArray();
                        HttpResponse httpResponse = HttpResponse.TryParse(responseBytes);

                        if (SelectedAlgorithmString == "CookieBased" && !httpRequest.HasHeader("Cookie"))
                        {
                            httpResponse.AddHeader("Set-Cookie", $"{selectedServer.Host}:{selectedServer.Port}");
                            responseBytes = httpResponse.ToBytes;
                        }

                        if(SelectedAlgorithmString == "SessionBased")
                        {
                            SetSession(incomingClient, selectedServer);
                        }

                        await incomingClientStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        
                        AddToLog($"Using server: {selectedServer.Host}:{selectedServer.Port}");

                        return httpResponse;
                    }
                    catch (Exception exception)
                    {
                        AddToLog(exception.Message);
                        byte[] responseBytes = HttpResponse.GetInternalServerErrorResponse().ToBytes;
                        HttpResponse httpResponse = HttpResponse.TryParse(responseBytes);
                        await incomingClientStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        return httpResponse;
                    }
                }
            }
        }

        private Server DetermineServer()
        {
            IAlgorithm algo = CurrentAlgorithm;
            if (algo == null || (SelectedAlgorithmString != PreviousAlgorithmString))
            {
                algo = CurrentAlgorithm = AlgoFactory.GetAlgorithm(SelectedAlgorithmString);
                PreviousAlgorithmString = SelectedAlgorithmString;
            }

            return algo.GetServer(Servers.Where((x) => x.Alive == true).ToList());
        }

        #region Persistence 
        private Server GetServerForCookie(HttpRequest httpRequest)
        {
            string value = httpRequest.GetHeader("Cookie").Value;
            if (value.Contains(";")){
                value = value.Substring(0, value.IndexOf(";"));
            }
            int index = value.IndexOf(":");
            string host = value.Substring(0, index);
            int port = int.Parse(value.Substring(index + 1, (value.Length - index - 1)));
            return Servers.Where((server) => server.Alive && server.Host == host && server.Port == port).First();
        }

        private Server GetServerForSession(TcpClient client)
        {
            try
            {
                IPEndPoint ipep = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress ipa = ipep.Address;

                Session ses = Sessions.Where((x) => x.ClientIP.ToString() == ipa.ToString()).FirstOrDefault();

                if (ses != null)
                {
                    return Servers.Where((x) => x.Host == ses.ServerIP && x.Port == ses.ServerPort && x.Alive == true).First();
                } else
                {
                    return null;
                }

            }
            catch (Exception e)
            {
                AddToLog(e.Message);
                return null;
            }
        }

        private void SetSession(TcpClient client, Server server)
        {
            try
            {
                IPEndPoint ipep = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress ipa = ipep.Address;
                Sessions.Add(new Session(ipa, server.Host, server.Port));
            }
            catch
            {
                AddToLog("Session could not be set.");
            }
        }
        #endregion

        private async void DoHealthCheck()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(HealthCheckInterval);
                    if (IsRunning)
                    {
                        if (Servers.Count() < 1) continue;
                        AddToLog("Performing Health Check!");
                        Servers.ForEach((server) =>
                        {
                            server.AskForHealth();
                        });

                        Servers.ForEach((server) =>
                        {
                            _dispatcher.Invoke(() => UpdateServerStatus(server));
                        });
                    }
                }
            });
        }

        private void UpdateServerStatus(Server server)
        {
            ListBoxItem serverItem = ServerList.Where((x) => x.Content.ToString().Contains($"{server.Host}:{server.Port}")).First();

            if (serverItem != null)
            {
                var color = server.Alive ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
                SolidColorBrush brush = new SolidColorBrush(color);
                serverItem.Foreground = brush;
            }
        }

        #region Commands
        public async void AddServer()
        {
            {
                bool conflict = false;

                if (ValidateNewServerPreferences(AddServerIP, AddServerPort))
                {
                    foreach (Server server in Servers)
                    {
                        if (server.Port == AddServerPort && server.Host == AddServerIP)
                        {
                            conflict = true;
                            break;
                        }
                    }

                    if (conflict)
                    {
                        AddToLog("ERROR: A server with this specification already exists.");
                        return;
                    }

                    Server newServer = new Server(AddServerIP, AddServerPort);
                    Servers.Add(newServer);

                    try
                    {
                        TcpClient client = new TcpClient();
                        await client.ConnectAsync(newServer.Host, newServer.Port);
                        newServer.Alive = true;
                        AddToLog($"{newServer.Host}:{newServer.Port} has been added and is accepting connections.");
                        _dispatcher.Invoke(() => AddToServerList($"{newServer.Host}:{newServer.Port}", true));
                    }
                    catch
                    {
                        _dispatcher.Invoke(() => AddToServerList($"{newServer.Host}:{newServer.Port}", false));
                        AddToLog($"{newServer.Host}:{newServer.Port} has been added but is refusing connections.");
                    }
                }
            }
        }

        public void RemoveServer(object item)
        {
            if (item is ListBoxItem)
            {
                ListBoxItem result = (ListBoxItem)item;
                ServerList.Remove(result);
                string content = (string)result.Content;
                string host = content.Split(':')[0];
                int port = int.Parse(content.Split(':')[1]);

                Server serverToRemove = Servers.Where((server) => server.Host == host && server.Port == port).First();

                if (serverToRemove != null)
                {
                    Servers.Remove(serverToRemove);
                    AddToLog($"{serverToRemove.Host}:{serverToRemove.Port} has been removed!");
                }
            }
            else
            {
                AddToLog("Something went wrong whilst deleting the server.");
            }
        }

        public void AddToServerList(string server, bool alive)
        {
            var color = alive ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
            SolidColorBrush brush = new SolidColorBrush(color);

            _dispatcher.Invoke(() => ServerList.Add(new ListBoxItem() { Content = server, Foreground = brush }));
        }

        public void InitAlgos()
        {
            SelectedAlgorithmString = null;
            Algorithms.Clear();
            IAlgorithmFactory.GetAllAlgorithms().ForEach((algo) =>
            {
                _dispatcher.Invoke(() => Algorithms.Add(new ListBoxItem { Content = algo }));
            });
            
        }

        private void AddToLog(string message) => _dispatcher.Invoke(() => Log.Add(new ListBoxItem { Content = message }));
        public void ClearLog() => Log.Clear();
        #endregion

        #region Validation
        private bool ValidateLoadbalancerPreferences(int portNumber, int bufferSize)
        {
            if (!(portNumber >= 1024 && portNumber <= 65535))
            {
                MessageBox.Show("Port has an invalid value or is not within the range of 1024 - 65535", "Invalid Port number", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (bufferSize <= 0)
            {
                MessageBox.Show("An invalid amount of buffer size has been given! Try something else.", "Invalid amount of Buffer Size", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (SelectedAlgorithmString == null)
            {
                MessageBox.Show("No Algorithm has been selected! Please select one.", "No algorithm selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Servers.Count() == 0)
            {
                MessageBox.Show("No Servers found! Please select at least one.", "No servers", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private bool ValidateNewServerPreferences(string ipAddress, int portNumber)
        {
            if (!ValidateIPv4(ipAddress))
            {
                MessageBox.Show("An invalid IP address has been given! Try another IP address", "Invalid IP address", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!(portNumber >= 1024 && portNumber <= 65535))
            {
                MessageBox.Show("Port has an invalid value or is not within the range of 1024 - 65535", "Invalid Port number", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }
    }
    #endregion
}
