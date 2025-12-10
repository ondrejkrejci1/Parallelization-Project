using System.Configuration;
using System.Net;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;

            var ipAddress = IPAddress.Parse(appSettings["IpAddress"]);
            int port = int.Parse(appSettings["Port"]);

            Server server = new Server(ipAddress,port);

            Console.WriteLine($"Server runs at: {ipAddress} - {port}");
            server.Start();

            while (true)
            {
                string? command = Console.ReadLine();
                if (command != null && command.ToLower() == "stop")
                {
                    server.Stop();
                    break;
                }
            }
        }
    }
}
