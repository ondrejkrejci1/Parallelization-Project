using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class Server
    {
        private TcpListener listener;
        private bool isRunning = false;
        private Thread clientAcceptor;
        private List<ClientHandler> clients;
        private List<string> images;

        public Server(IPAddress ipaddress, int port)
        {
            listener = new TcpListener(ipaddress, port);
            clients = new List<ClientHandler>();
            clientAcceptor = new Thread(AcceptClient);
            LoadImages();
        }

        public void Start()
        {
            listener.Start();
            isRunning = true;
            clientAcceptor.Start();
            Console.WriteLine("Server started.");
        }
        public void Stop()
        {
            try
            {
                listener.Stop();
                isRunning = false;
                Console.WriteLine("Server stopped.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


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

        public void AddClient(ClientHandler clientHandler)
        {
            lock (clients)
            {
                clients.Add(clientHandler);
            }
        }

        public void RemoveClient(ClientHandler clientHandler)
        {
            lock (clients)
            {
                clients.Remove(clientHandler);
            }
        }

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

        public void RegisterImage(string imageName)
        {
            images.Add(imageName);
        }

        public List<string> GetRegisteredImages()
        {
            return images;
        }
    }
}
