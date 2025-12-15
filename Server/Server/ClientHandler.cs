using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class ClientHandler
    {
        public TcpClient Client { get; private set; }
        private Thread clientHandler;
        public string Name { get; private set; }
        public StreamReader Reader { get; private set; }
        public StreamWriter Writer { get; private set; }
        public Server Server { get; private set; }
        private ClientConsole console;

        public ClientHandler(TcpClient client, Server server)
        {
            Client = client;
            Server = server;
            clientHandler = new Thread(Run);
            clientHandler.Start();
        }

        private void Run()
        {
            try
            {
                Reader = new StreamReader(Client.GetStream(), Encoding.UTF8);
                Writer = new StreamWriter(Client.GetStream(), Encoding.UTF8) { AutoFlush = true };

                Server.AddClient(this);

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
                Client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing connection for {Name}: {ex.Message}");
            }
        }
    }

}
