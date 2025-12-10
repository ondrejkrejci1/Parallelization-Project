using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class ClientHandler
    {
        private TcpClient client;
        private Thread clientHandler;
        public string Name { get; private set; }
        public StreamReader Reader { get; private set; }
        public StreamWriter Writer { get; private set; }
        private Server server;
        private ClientConsole console;

        public ClientHandler(TcpClient client, Server server)
        {
            this.client = client;
            this.server = server;
            clientHandler = new Thread(Run);
            clientHandler.Start();
        }

        private void Run()
        {
            try
            {
                Reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                Writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };

                server.AddClient(this);

                Writer.WriteLine("Welcome to Server for image sharing!");

                console = new ClientConsole(this);
                console.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in client handler: {ex.Message}");
            }
        }

        private void SendMessage(string message)
        {
            try
            {
                Writer.WriteLine(message); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to {Name}: {ex.Message}");
            }
        }

        private void Stop()
        {
            try
            {
                Reader.Close();
                Writer.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing connection for {Name}: {ex.Message}");
            }
        }
    }

}
