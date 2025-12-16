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
                if (!Directory.Exists(UploadDirectory)) Directory.CreateDirectory(UploadDirectory);

                DirectoryInfo di = new DirectoryInfo(UploadDirectory);
                var files = di.GetFiles().Select(f => $"{f.Name}:{f.Length}").ToArray();

                if (files.Length == 0)
                {
                    clientHandler.Writer.WriteLine("ERROR:No images available.");
                    return "No images to offer.";
                }

                string manifest = string.Join("|", files);
                clientHandler.Writer.WriteLine($"FILE_LIST:{manifest}");
                clientHandler.Writer.Flush();

                string fileName = clientHandler.Reader.ReadLine();
                if (string.IsNullOrEmpty(fileName)) return "Download cancelled by client.";

                string fullPath = Path.Combine(UploadDirectory, fileName);

                if (File.Exists(fullPath))
                {
                    using (FileStream fs = File.OpenRead(fullPath))
                    {
                        NetworkStream networkStream = clientHandler.Client.GetStream();

                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            networkStream.Write(buffer, 0, bytesRead);
                            Thread.Sleep(100);
                        }
                        networkStream.Flush();
                    }
                    return $"File '{fileName}' sent.";
                }
                else
                {
                    clientHandler.Writer.WriteLine("ERROR:File no longer exists.");
                    return "File not found.";
                }
            }
            catch (Exception ex)
            {
                clientHandler.Writer.WriteLine("ERROR:Server error.");
                return $"Error: {ex.Message}";
            }
        }

        public bool Exit()
        {
            return false;
        }

    }
}
