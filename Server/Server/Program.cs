using System.Configuration;
using System.Net;

namespace Server
{
    /// <summary>
    /// Represents the entry point of the server application.
    /// Responsible for reading configuration, initializing the server, and handling the application lifecycle.
    /// </summary>
    public class Program
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
