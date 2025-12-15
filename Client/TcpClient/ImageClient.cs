using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Drawing;

namespace TcpClient
{
    public class ImageClient
    {
        private Thread receiveMessages;
        private bool isRunning = true;
        private StreamReader reader;
        private StreamWriter writer;
        private System.Net.Sockets.TcpClient client;
        private string lastMessage;
        private bool pauseReadCylce = false;

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

                using (client = new System.Net.Sockets.TcpClient())
                {
                    client.Connect(ip, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        reader = new StreamReader(stream, Encoding.UTF8);  //prijima zpravy
                        writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true }; //posila zpravy

                        receiveMessages = new Thread(ReceiveLoop);
                        receiveMessages.Start();

                        bool uploadMode = false;
                        bool downloadMode = false;

                        while (isRunning)
                        {
                            if (uploadMode)
                            {
                                UploadImage();
                                uploadMode = false;
                            }
                            else if (downloadMode)
                            {
                                DownloadImage();
                                downloadMode = false;
                            }
                            else
                            {
                                Thread.Sleep(10);
                                Console.Write(">> ");
                                string message = Console.ReadLine().Trim();

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
                                else if (message.ToLower() == "downloadimage")
                                {
                                    downloadMode = true;
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
                    lastMessage = serverMessage;

                    if (serverMessage == null)
                    {
                        Console.WriteLine("\n[SERVER CLOSED THIS CONNECTION]");
                        break;
                    }
                    if (pauseReadCylce == false)
                    {
                        Console.Write(serverMessage + "\n");
                    }
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

        private void UploadImage()
        {
            string fileNameAndSize = "";
            bool fileNameReady = false;

            while (!fileNameReady)
            {
                fileNameAndSize = Console.ReadLine();

                string[] specification = fileNameAndSize.Split(' ', 2);

                string[] suffixes = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".svg" };

                if (specification.Length == 2 || long.TryParse(specification[1], out long expectedSize))
                {
                    foreach (string suffix in suffixes)
                    {
                        if (specification[0].EndsWith(suffix))
                        {
                            fileNameReady = true;
                        }
                    }

                    if (!fileNameReady)
                    {
                        Console.Write("Check if you include suffix in your file name. \nEntry again: ");
                    }
                }
            }

            SendMessage(fileNameAndSize);
            Thread.Sleep(100);

            if (lastMessage.StartsWith("ERROR:"))
            {
                return;
            }

            string fullPath;

            while (true)
            {
                fullPath = FileExists(fileNameAndSize.Split(' ', 2)[0]);

                if (IsImageFile(fullPath) == true)
                {
                    break;
                }
                Console.WriteLine("The file is not a image.");
            }
            

            byte[] imageData = File.ReadAllBytes(fullPath);
            long fileSize = imageData.LongLength;
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(imageData, 0, imageData.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while uploading: {ex.Message}");
            }

            if (lastMessage.StartsWith("SUCCESS:"))
            {
                Console.WriteLine("Image uploaded successfully.");
            }
            else if (lastMessage.StartsWith("ERROR:"))
            {
                Console.WriteLine("Error occurred during image upload.");
            }

        }

        private string FileExists(string fileName)
        {

            string fullPath = "";
            bool fileExists = false;

            while (!fileExists)
            {
                Console.WriteLine("Enter full path of image: ");
                fullPath = Console.ReadLine().Trim();

                if (fullPath == "exit")
                {
                    return "exit";
                }


                if (File.Exists(fullPath) )
                {
                    fileExists = true;
                }
                if (File.Exists(Path.Combine(fullPath,fileName)))
                {
                    fullPath = Path.Combine(fullPath, fileName);
                    fileExists = true;
                }

            }

            return fullPath;
        }
        private bool IsImageFile(string path)
        {
            try
            {
                using (Image img = Image.FromFile(path))
                {
                    return true;
                }
            }
            catch (OutOfMemoryException)
            {
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void DownloadImage()
        {
            // Pamatuj, že klient již poslal pøíkaz "downloadimage"
            // Nyní se oèekává zpráva FILE_LIST od serveru.

            Console.WriteLine("Waiting for file list from server...");

            // Doèasnì pozastavíme standardní výpis zpráv z ReceiveLoop
            pauseReadCylce = true;

            string fileListMessage = "";
            string fileListPrefix = "FILE_LIST:";

            // Èekáme na zprávu, která zaèíná prefixem FILE_LIST:
            // Použijeme krátkou smyèku a timeout, aby se vlákno neblokovalo navždy.
            int timeoutMs = 5000;
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                if (lastMessage != null && lastMessage.StartsWith("<< " + fileListPrefix))
                {
                    // Odebereme prefix '<< ' a 'FILE_LIST:'
                    fileListMessage = lastMessage.Substring(3 + fileListPrefix.Length);
                    break;
                }
                Thread.Sleep(100);
                elapsed += 100;
            }

            if (string.IsNullOrEmpty(fileListMessage))
            {
                Console.WriteLine("Download failed: Did not receive file list from server.");
                pauseReadCylce = false;
                return;
            }

            // 1. Zobrazení seznamu a výbìr souboru
            string[] availableFiles = fileListMessage.Split('|');

            Console.WriteLine("\n--- Available Images ---");
            for (int i = 0; i < availableFiles.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {availableFiles[i]}");
            }
            Console.WriteLine("------------------------");

            Console.Write("Enter the name of the image to download (e.g., photo.jpg): ");
            string fileNameToDownload = Console.ReadLine().Trim();

            if (string.IsNullOrEmpty(fileNameToDownload))
            {
                Console.WriteLine("Download cancelled by user.");
                pauseReadCylce = false;
                return;
            }

            // 2. Odeslání vybraného jména serveru
            SendMessage(fileNameToDownload);

            // 3. Èekání na zprávu FILE_READY nebo chybu
            string fileReadyPrefix = "FILE_READY:";
            string responseMessage = "";
            long fileSize = 0;
            string receivedFileName = "";

            // Resetujeme èasovaè pro èekání na FILE_READY
            elapsed = 0;
            timeoutMs = 5000;

            while (elapsed < timeoutMs)
            {
                // Kontrola lastMessage (z ReceiveLoop)
                if (lastMessage != null && lastMessage.StartsWith("<< " + fileReadyPrefix))
                {
                    responseMessage = lastMessage.Substring(3 + fileReadyPrefix.Length);

                    // Oèekáváme formát: název_souboru:velikost
                    string[] parts = responseMessage.Split(':');

                    if (parts.Length == 2 && long.TryParse(parts[1], out fileSize))
                    {
                        receivedFileName = parts[0];
                        break;
                    }
                }
                else if (lastMessage != null && (lastMessage.Contains("FILE_NOT_FOUND") || lastMessage.Contains("DOWNLOAD_ERROR")))
                {
                    // Server odeslal chybu, kterou již ReceiveLoop vypsal.
                    Console.WriteLine("Download was unsuccessful (File not found or server error).");
                    pauseReadCylce = false;
                    return;
                }
                Thread.Sleep(100);
                elapsed += 100;
            }

            if (fileSize == 0) // Znamená, že jsme timeoutnuli nebo neobdrželi správný prefix
            {
                Console.WriteLine("Download failed: Server did not respond with file information or size was zero.");
                pauseReadCylce = false;
                return;
            }

            // 4. Vyžádání cesty k uložení
            string savePath = PathExists(); // Použijeme stávající metodu pro ovìøení cesty
            string fullSavePath = Path.Combine(savePath, receivedFileName);

            Console.WriteLine($"Starting download of {receivedFileName} ({fileSize} bytes)...");

            try
            {
                // Zde se provádí binární pøenos
                NetworkStream stream = client.GetStream();

                // Použijeme existující metodu pro bezpeèné pøeètení dat
                byte[] imageData = ReadBytesFromStream(stream, fileSize);

                // 5. Uložení souboru
                File.WriteAllBytes(fullSavePath, imageData);

                Console.WriteLine($"\nFile successfully downloaded and saved to: {fullSavePath}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR during download process: {ex.Message}");
            }
            finally
            {
                // Obnovení standardního výpisu zpráv
                pauseReadCylce = false;
            }

            // Po úspìšném/neúspìšném pøenosu, Server odešle finální textovou zprávu,
            // kterou zachytí ReceiveLoop. Zde již konèíme DownloadImage.
        }

        // Dále je nutné upravit ReceiveLoop, aby ne vždy vypisoval zprávu, pokud je pauznut

        // ... (ostatní metody) ...
    }

    private byte[] ReadBytesFromStream(NetworkStream stream, long count)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                long bytesRemaining = count;
                int bytesRead;

                // Klíèové: Tento cyklus musí bìžet, dokud nepøeète všechna data.
                // Bude se blokovat (èekat), pokud data ještì nedorazila,
                // ale v tomto pøípadì by mìl server poslat všechna data najednou.
                while (bytesRemaining > 0)
                {
                    // Pøeèti maximálnì velikost bufferu, nebo co zbývá
                    int maxRead = (int)Math.Min(buffer.Length, bytesRemaining);

                    bytesRead = stream.Read(buffer, 0, maxRead);

                    if (bytesRead == 0)
                    {
                        // Pokud stream.Read vrátí 0, spojení bylo uzavøeno.
                        throw new IOException("Connection lost during data transfer.");
                    }

                    ms.Write(buffer, 0, bytesRead);
                    bytesRemaining -= bytesRead;
                }

                // Kontrola, že bytesRemaining je 0, je zbyteèná, protože cyklus skonèil,
                // ale ponech ji, pokud chceš zajistit absolutní jistotu.
                if (bytesRemaining != 0)
                {
                    // Tato èást by se nemìla nikdy spustit, pokud cyklus dobìhl
                    throw new InvalidOperationException("Internal buffer error.");
                }

                return ms.ToArray();
            }
        }

        /*
        private byte[] ReadBytesFromStream(NetworkStream stream, long count)
        {
            
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                long bytesRemaining = count;
                int bytesRead;

                while (bytesRemaining > 0 && (bytesRead = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesRemaining))) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                    bytesRemaining -= bytesRead;
                    Console.WriteLine(bytesRemaining);
                }

                if (bytesRemaining != 0)
                {
                    throw new IOException("Connection lost, or data was incomplete.");
                }
                
                return ms.ToArray();
            }
        }
        */
        private string PathExists()
        {
            bool pathExists = false;
            string fullPath = "";

            while (!pathExists)
            {
                Console.WriteLine("Enter full path where you want to save the image: ");
                fullPath = Console.ReadLine().Trim();

                if (Directory.Exists(fullPath))
                {
                    pathExists = true;
                }
            }

            return fullPath;
        }
    }

}


