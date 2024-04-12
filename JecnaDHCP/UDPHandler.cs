using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;

namespace JecnaDHCP
{
    public class UDPHandler
    {
        public readonly IPEndPoint EndPoint;
        public IPAddress ListeningAddress => EndPoint.Address;
        public ushort PortNumber => (ushort)EndPoint.Port;

        private Thread _thread;
        private ConcurrentDictionary<IPAddress, string> _receivedOffers = new ConcurrentDictionary<IPAddress, string>();

        public bool IsRunning;

        public UDPHandler(ushort portNumber, IPAddress listeningAddress)
        {
            EndPoint = new IPEndPoint(listeningAddress, portNumber);
        }

        public void Init()
        {
            IsRunning = true;

            _thread = new Thread(Loop);
            _thread.Start();
        }

        private async void Loop()
        {
            try
            {
                using (UdpClient udpc = new UdpClient(new IPEndPoint(ListeningAddress, PortNumber)))
                {
                    udpc.EnableBroadcast = true;
                    Console.WriteLine($"Handler Started, servicing on port {PortNumber}");

                    await SendData(udpc, Encoding.UTF8.GetBytes("JECNADISCOVER"));

                    while (IsRunning)
                    {
                        UdpReceiveResult res = await udpc.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(res.Buffer);

                        await Console.Out.WriteLineAsync($"Received {message}");

                        if (message == "JECNADISCOVER")
                        {
                            byte[] response = Encoding.UTF8.GetBytes("JECNAOFFER");
                            byte[] hostname = Encoding.UTF8.GetBytes(Dns.GetHostName());

                            await SendData(udpc, response);
                            await SendData(udpc, hostname);

                            await Console.Out.WriteLineAsync($"Sending discover message to {res.RemoteEndPoint.Address}");
                        }
                        else if (message == "JECNAOFFER")
                        {
                            _receivedOffers.TryAdd(res.RemoteEndPoint.Address, string.Empty);

                            await Console.Out.WriteLineAsync($"Received offer from {res.RemoteEndPoint.Address}");
                        }
                        else if (_receivedOffers.TryGetValue(res.RemoteEndPoint.Address, out string val) && string.IsNullOrEmpty(val))
                        {
                            string hostname = Encoding.UTF8.GetString(res.Buffer);
                            _receivedOffers[res.RemoteEndPoint.Address] = hostname;

                            await Console.Out.WriteLineAsync($"Received additional hostname from {res.RemoteEndPoint.Address} as {hostname}");

                            await Console.Out.WriteLineAsync("All offers:");
                            foreach (var kvp in _receivedOffers)
                            {
                                await Console.Out.WriteLineAsync($"Address: {kvp.Key}, Hostname: {kvp.Value}");
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync(e.ToString());
            }
        }

        private async Task SendData(UdpClient udpc, byte[] data, IPEndPoint point = null)
        {
            await udpc.SendAsync(data, data.Length, point ?? EndPoint);
        }
    }
}
