using System.Net.Sockets;
using System.Text;

namespace Server
{
    /// <summary>
    /// Manages the lifecycle and communication for a specific connected TCP client.
    /// It runs in its own thread, initializes the network streams, and delegates 
    /// command processing to the ClientConsole.
    /// </summary>
    public class ClientHandler
    {
        /// <summary>
        /// Gets the underlying TCP client connection.
        /// </summary>
        public TcpClient Client { get; private set; }
        private Thread clientHandler;

        /// <summary>
        /// Gets the name or identifier of the client.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the stream reader used for reading incoming text messages from the client.
        /// </summary>
        public StreamReader Reader { get; private set; }

        /// <summary>
        /// Gets the stream writer used for sending text messages to the client.
        /// </summary>
        public StreamWriter Writer { get; private set; }

        /// <summary>
        /// Gets the reference to the main server instance.
        /// </summary>
        public Server Server { get; private set; }
        private ClientConsole console;

        /// <summary>
        /// Initializes a new instance of the ClientHandler class.
        /// Immediately starts a new background thread to handle the client connection.
        /// </summary>
        /// <param name="client">The connected TCP client instance.</param>
        /// <param name="server">The main server instance managing this handler.</param>
        public ClientHandler(TcpClient client, Server server)
        {
            Client = client;
            Server = server;
            clientHandler = new Thread(Run);
            clientHandler.Start();
        }

        /// <summary>
        /// The main execution loop for the client thread.
        /// It initializes streams, registers the client with the server, sends a welcome message, 
        /// and starts the ClientConsole for command interpretation.
        /// </summary>
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

        /// <summary>
        /// Safely terminates the client connection.
        /// Stops the command console, closes the network streams, and closes the socket connection.
        /// </summary>
        public void Stop()
        {
            try
            {
                console.Stop();
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
