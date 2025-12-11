using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;
using System.Net.Http;

namespace TcpClient
{
    public class ImageClient
    {
        private Thread receiveMessages;
        private bool isRunning = true;

        private StreamReader reader;
        private StreamWriter writer;
        private System.Net.Sockets.TcpClient client;

        public ImageClient()
        {
        }

        public void Connect()
        {

            try
            {
                var appSettings = ConfigurationManager.AppSettings;

                IPAddress ip = IPAddress.Parse(appSettings["IpAddress"]);
                int port = int.Parse(appSettings["Port"]);

                using (this.client = new System.Net.Sockets.TcpClient())
                {
                    client.Connect(ip, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        this.reader = new StreamReader(stream, Encoding.UTF8);  //prijima zpravy
                        this.writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true }; //posila zpravy

                        receiveMessages = new Thread(ReceiveLoop);
                        receiveMessages.Start();

                        bool uploadMode = false;
                        bool downloadMode = false;

                        while (isRunning)
                        {
                            if (uploadMode)
                            {
                                string nameAndSize = Console.ReadLine();
                                writer.WriteLine(nameAndSize);
                                
                                Console.WriteLine("Enter the full path to the image file to upload:\n>> ");
                                string path = Console.ReadLine();

                                string nameAndPath = nameAndSize.Split(' ')[0].Trim() + " " + path.Trim();
                            }
                            else if (downloadMode)
                            {
                                
                            }
                            else
                            {
                                Console.Write(">> ");
                                string message = Console.ReadLine();
                                message = message.Trim();

                                if (string.IsNullOrEmpty(message)) continue;

                                if (message.ToLower() == "exit")
                                {
                                    SendMessage(message);
                                    isRunning = false;
                                    receiveMessages.Join();
                                    reader?.Dispose();
                                    writer?.Dispose();
                                    break;
                                }
                                else if (message.ToLower() == "uploadimage")
                                {
                                    uploadMode = true;
                                }

                                SendMessage(message);
                            }                            
                            
                        }


                        Console.WriteLine("\nPress Enter to exit.");
                        Console.ReadLine();
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Server is probably unavailable. Please check settings inside app.config file if everything is allright.");
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

        private void ReceiveLoop()
        {
            try
            {
                while (isRunning)
                {
                    string serverMessage = reader.ReadLine();

                    if (serverMessage == null)
                    {
                        Console.WriteLine("\n[SERVER CLOSED THIS CONNECTION]");
                        break;
                    }

                    Console.Write("\n" + serverMessage);
                }
            }
            catch (IOException)
            {
                if (isRunning)
                {
                    Console.WriteLine("\nCONNECTION WAS FORCIBLY TERMINATED");
                }
            }
            catch (ObjectDisposedException)
            {

            }
        }

        private void SendMessage(string message)
        {
            try
            {
                writer.WriteLine(message);
            }
            catch (Exception)
            {
                Console.WriteLine("Could not reach the server. Probably there is a problem with connection.");
                isRunning = false;
            }
        }

        public void UploadImage(string commandInput)
        {
            // Očekávaný vstup od uživatele: "uploadimage C:\cesta\k\souboru.jpg"
            string[] parts = commandInput.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                throw new InvalidDataException("Error- Usage: uploadimage <local_file_path>");
            }

            string localFilePath = parts[1].Trim();

            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException("Local file not found.", localFilePath);
            }

            byte[] imageData = File.ReadAllBytes(localFilePath);
            long fileSize = imageData.LongLength;
            string fileName = Path.GetFileName(localFilePath);

            // Zpráva pro druhou fázi handshaku
            string specificationMessage = $"{fileName} {fileSize}";

            try
            {
                // ----------------------------------------------------
                // FÁZE 1: INICIACE PŘÍKAZU
                // Klient musí poslat jen název příkazu, aby server věděl, co má spustit
                SendMessage(parts[0]); // Odešleme pouze "uploadimage"

                // Server nyní spustil Execute() a BLOKUJE na prvním ReadLine()

                // ----------------------------------------------------
                // FÁZE 2: ODPOVĚĎ NA VÝZVU SERVERU

                // Klient musí přečíst výzvu (např. "Enter your image specification...")
                Console.WriteLine("[CLIENT] Čekám na výzvu serveru...");

                // POZOR: Musíme dočasně blokovat a číst v HLAVNÍM vlákně,
                // aby byla zpráva serveru přečtena HNED.
                // Normální příjem v ReceiveLoop by mohl být pomalý.

                string serverPrompt = reader.ReadLine();
                if (serverPrompt == null) throw new IOException("Spojení bylo ukončeno.");
                Console.WriteLine($"[SERVER PROMPT] {serverPrompt.Replace("\n>> ", "")}");

                // Nyní pošleme specifikaci, kterou server očekává na svém prvním ReadLine()
                SendMessage(specificationMessage);
                Console.WriteLine($"[CLIENT] Odeslal specifikaci: {specificationMessage}");

                // ----------------------------------------------------
                // FÁZE 3: PŘÍJEM POTVRZENÍ

                // Čekáme na potvrzení serveru, že je připraveno ("Ready to receive...")
                string serverReady = reader.ReadLine();
                if (serverReady == null) throw new IOException("Spojení bylo ukončeno po specifikaci.");
                Console.WriteLine($"[SERVER CONFIRMATION] {serverReady}");

                // Zde kontrola, zda zpráva obsahuje "Ready to receive"
                if (!serverReady.StartsWith("Ready to receive"))
                {
                    throw new InvalidDataException("Server did not confirm readiness to receive data.");
                }

                // ----------------------------------------------------
                // FÁZE 4: BINÁRNÍ PŘENOS DAT

                NetworkStream stream = client.GetStream();

                Console.WriteLine($"[CLIENT] Posílám {fileSize} bytů binárních dat...");
                stream.Write(imageData, 0, imageData.Length);
                stream.Flush();

                Console.WriteLine("[CLIENT] Data odeslána. Čekám na konečnou odpověď (SUCCESS/ERROR)...");

                // Konečnou zprávu (SUCCESS/ERROR) zachytí a vypíše vlákno ReceiveLoop.

            }
            catch (Exception ex)
            {
                Console.WriteLine($"🛑 CHYBA UPLOADU: {ex.Message}");
            }
        }


    }

}

