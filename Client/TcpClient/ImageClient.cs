using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;

namespace TcpClient
{
    public class ImageClient
    {
        public void Connect()
        {

            try
            {
                var appSettings = ConfigurationManager.AppSettings;

                IPAddress ip = IPAddress.Parse(appSettings["IpAddress"]);
                int port = int.Parse(appSettings["Port"]);

                using (var client = new System.Net.Sockets.TcpClient())
                {
                    client.Connect(ip, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        SendTestMessage(stream, "Test SEND MESSSAGE");
                        ReceiveResponse(stream);

                        Console.WriteLine("\nPress Enter to exit.");
                        Console.ReadLine();
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Server is probably unavailable. Please check settings inside app.config file.");
                Console.WriteLine($"Detail: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Client has left the connection.");
            }

        }

        private static void SendTestMessage(NetworkStream stream, string message)
        {
            Console.WriteLine($"\n<< Responce: {message}");
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Posíláme data na server
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private static void ReceiveResponse(NetworkStream stream)
        {
            // Kontrolujeme, zda stream obsahuje data.
            if (stream.DataAvailable)
            {
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"\n>> Server answer: {response}");
            }
            else
            {
                // Dáme serveru malou chvíli na odpověď (při testování)
                Thread.Sleep(100);
                if (stream.DataAvailable)
                {
                    ReceiveResponse(stream); // Zkusíme znovu
                }
                else
                {
                    Console.WriteLine(">> Server does not responde.");
                }
            }
        }
    }

}

