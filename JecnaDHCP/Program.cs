using System.Net;

namespace JecnaDHCP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IPAddress address = null;
            ushort port = 0;

            if (args.Length == 2)
            {
                address = IPAddress.Parse(args[0]);
                port = ushort.Parse(args[1]);
            }

            address ??= IPAddress.Parse("172.31.255.255");
            port = (ushort)(port == 0 ? 666 : port);

            Console.WriteLine($"IP: {address}\nPort: {port}");

            Console.WriteLine("Starting handler");

            UDPHandler server = new UDPHandler(port, address);
            server.Init();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
