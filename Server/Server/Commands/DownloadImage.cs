using System.Net.Sockets;

namespace Server.Commands
{
    /// <summary>
    /// Handles the "downloadimage" command, allowing the client to retrieve files from the server.
    /// This command manages the entire download flow: listing available files, accepting the client's selection,
    /// and streaming the binary file data over the network.
    /// </summary>
    public class DownloadImage : ICommand
    {
        private ClientHandler clientHandler;
        /// <summary>
        /// The local directory where uploaded images are saved.
        /// </summary>
        private const string UploadDirectory = "UploadedImages";

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadImage"/> class.
        /// </summary>
        /// <param name="client">The handler for the connected client requesting the download.</param>
        public DownloadImage(ClientHandler client)
        {
            this.clientHandler = client;
        }

        /// <summary>
        /// Executes the download protocol.
        /// 1. Scans the 'UploadedImages' directory and generates a manifest string (filename:size).
        /// 2. Sends the manifest to the client (prefixed with "FILE_LIST:").
        /// 3. Waits for the client to reply with the desired filename.
        /// 4. Opens the file and streams its content to the client using a buffer.
        /// </summary>
        /// <returns>A status message indicating the result of the operation (e.g., "File sent", "File not found").</returns>
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

                    clientHandler.Server.Logger.Log($"User {clientHandler.Name}: Downloaded image - {fileName}");

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

        /// <summary>
        /// Indicates that the client session should remain active after the download is complete.
        /// </summary>
        /// <returns>Always returns false.</returns>
        public bool Exit()
        {
            return false;
        }

    }
}
