using System.Net.Sockets;

namespace Server.Commands
{
    public class DownloadImage : ICommand
    {
        private ClientHandler clientHandler;
        private const string UploadDirectory = "UploadedImages";

        public DownloadImage(ClientHandler client)
        {
            this.clientHandler = client;
        }

        public string Execute()
        {
            try
            {
                // 1. Získání seznamu souborů
                string[] files = Directory.GetFiles(UploadDirectory)
                                          .Select(Path.GetFileName)
                                          .ToArray();

                if (files.Length == 0)
                {
                    return "No images available for download.";
                }

                // 2. Odeslání seznamu souborů klientovi
                string fileList = string.Join("|", files); // Oddělovač pro seznam souborů
                // Klient obdrží zprávu ve formátu "FILE_LIST:file1.jpg|file2.png|..."
                clientHandler.Writer.WriteLine($"FILE_LIST:{fileList}");
                clientHandler.Writer.Flush();

                // 3. Očekávání jména souboru od klienta
                // Čteme z Readeru, který klient po odeslání listu pošle.
                string fileName = clientHandler.Reader.ReadLine();
                if (string.IsNullOrEmpty(fileName))
                {
                    return "No file name received from client.";
                }

                string fullPath = Path.Combine(UploadDirectory, fileName);

                if (File.Exists(fullPath))
                {
                    // 4. Poslat potvrzení a velikost souboru
                    long fileSize = new FileInfo(fullPath).Length;
                    // Odesíláme speciální zprávu, aby se klient připravil na binární přenos.
                    // Formát: "FILE_READY:název_souboru:velikost"
                    clientHandler.Writer.WriteLine($"FILE_READY:{fileName}:{fileSize}");
                    clientHandler.Writer.Flush();

                    // 5. Odeslání souboru binárně
                    using (FileStream fs = File.OpenRead(fullPath))
                    {
                        // Používáme NetworkStream pro binární přenos
                        NetworkStream networkStream = clientHandler.Client.GetStream();
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            networkStream.Write(buffer, 0, bytesRead);
                        }
                    }

                    // Po úspěšném přenosu, vrátíme textovou zprávu pro ClientConsole (na serveru)
                    return $"File '{fileName}' sent successfully.";
                }
                else
                {
                    // 6. Odeslání zprávy o nenalezení souboru
                    clientHandler.Writer.WriteLine("FILE_NOT_FOUND");
                    clientHandler.Writer.Flush();
                    return $"File '{fileName}' not found on server.";
                }
            }
            catch (Exception ex)
            {
                // V případě chyby zrušíme případné čekání klienta
                clientHandler.Writer.WriteLine("DOWNLOAD_ERROR");
                clientHandler.Writer.Flush();
                return $"Error during image download: {ex.Message}";
            }
        }

        public bool Exit()
        {
            return false;
        }
    
    }
}
