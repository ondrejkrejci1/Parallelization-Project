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


        public Server(IPAddress ipaddress, int port)
        {
            listener = new TcpListener(ipaddress, port);
            clients = new List<ClientHandler>();
            clientAcceptor = new Thread(AcceptClient);
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



    }
}
