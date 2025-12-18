using System.Net;
using System.Net.Sockets;

namespace Server
{
    /// <summary>
    /// Represents the main TCP server that handles incoming client connections, 
    /// manages the list of active clients, and maintains a registry of available images.
    /// </summary>
    public class Server
    {
        private TcpListener listener;
        private bool isRunning = false;
        private Thread clientAcceptor;
        private List<ClientHandler> clients;
        private List<string> images;
        public Logger Logger { get; private set; }

        private IPAddress ipaddress;
        private int port;

        /// <summary>
        /// Initializes a new instance of the Server class.
        /// Sets up the TCP listener and loads existing images from the storage directory.
        /// </summary>
        /// <param name="ipaddress">The IP address to bind the server to.</param>
        /// <param name="port">The port number to listen on.</param>
        public Server(IPAddress ipaddress, int port)
        {
            this.ipaddress = ipaddress;
            this.port = port;
            Logger = new Logger();
            listener = new TcpListener(ipaddress, port);
            clients = new List<ClientHandler>();
            clientAcceptor = new Thread(AcceptClient);
            LoadImages();
        }

        /// <summary>
        /// Starts the TCP listener and the background thread responsible for accepting incoming client connections.
        /// </summary>
        public void Start()
        {
            listener.Start();
            isRunning = true;
            clientAcceptor.Start();
            PrintMessage($"Server started at {ipaddress}:{port}");
        }

        /// <summary>
        /// Stops the TCP listener and sets the running flag to false.
        /// </summary>
        public void Stop()
        {
            try
            {
                foreach(var client in clients)
                {
                    client.Stop();
                }
                listener.Stop();
                isRunning = false;
                Console.WriteLine("Server stopped.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Background loop that continuously waits for and accepts pending connection requests.
        /// When a client connects, a new ClientHandler is created to handle communication.
        /// </summary>
        private void AcceptClient()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();

                    _ = new ClientHandler(client, this);

                }
                catch (SocketException socketEx)
                {

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }

            }
        }

        /// <summary>
        /// Adds a connected client handler to the internal list of active clients.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="clientHandler">The client handler instance to add.</param>
        public void AddClient(ClientHandler clientHandler)
        {
            lock (clients)
            {
                clients.Add(clientHandler);
            }
        }

        /// <summary>
        /// Removes a disconnected client handler from the internal list.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="clientHandler">The client handler instance to remove.</param>
        public void RemoveClient(ClientHandler clientHandler)
        {
            lock (clients)
            {
                clients.Remove(clientHandler);
            }
        }

        /// <summary>
        /// Scans the "UploadedImages" directory and populates the internal list with existing file names.
        /// Called during server initialization.
        /// </summary>
        private void LoadImages()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UploadedImages");

            images = new List<string>();

            try
            {
                string[] fullPathsToImages = Directory.GetFiles(path);

                string[] imageNames = new string[fullPathsToImages.Length];

                for (int i = 0; i < fullPathsToImages.Length; i++)
                {
                    imageNames[i] = Path.GetFileName(fullPathsToImages[i]);
                }


                if (imageNames.Length > 0)
                {
                    foreach (string name in imageNames)
                    {
                        images.Add(name);
                    }
                }

            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: Permision denied for reading the images name.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while reading the images name: {ex.Message}");
            }

        }

        /// <summary>
        /// Registers a new image filename in the server's memory.
        /// Should be called after a successful file upload to update the available file list immediately.
        /// </summary>
        /// <param name="imageName">The name of the file (including extension) to register.</param>
        public void RegisterImage(string imageName)
        {
            images.Add(imageName);
        }

        /// <summary>
        /// Retrieves the list of names of all images currently available on the server.
        /// </summary>
        /// <returns>A list of strings representing the filenames.</returns>
        public List<string> GetRegisteredImages()
        {
            return images;
        }

        /// <summary>
        /// Displays a message on the server console.
        /// </summary>
        /// <param name="message">Message to be displayed</param>
        public void PrintMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
