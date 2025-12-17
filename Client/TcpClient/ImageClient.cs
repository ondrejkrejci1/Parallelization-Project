using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Drawing;

namespace TcpClient
{
    /// <summary>
    /// Represents a TCP client capable of sending text messages, uploading images, 
    /// and downloading images to/from a remote server.
    /// </summary>
    public class ImageClient
    {
        private Thread receiveMessages;
        private bool isRunning = true;
        private StreamReader reader;
        private StreamWriter writer;
        private System.Net.Sockets.TcpClient client;
        private string lastMessage;

        /// <summary>
        /// Flag to pause the standard text reading loop. 
        /// Used when expecting binary data transfer (downloading) to prevent the text reader from consuming the stream.
        /// </summary>
        private bool pauseReadCylce = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageClient"/> class.
        /// </summary>
        public ImageClient()
        {
        }

        /// <summary>
        /// Establishes a connection to the server defined in the application configuration.
        /// handling the main input loop for sending messages and triggering file transfers.
        /// </summary>
        /// <exception cref="SocketException">Thrown when the client cannot connect to the server.</exception>
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

        /// <summary>
        /// Background thread method that continuously listens for incoming text messages from the server.
        /// Skips reading if pauseReadCylce is set to true (during binary downloads).
        /// </summary>
        private void ReceiveLoop()
        {
            try
            {
                while (isRunning)
                {
                    try
                    {
                        if (pauseReadCylce == false)
                        {
                            string serverMessage = reader.ReadLine();
                            lastMessage = serverMessage;

                            if (serverMessage == null)
                            {
                                Console.WriteLine("\n[SERVER CLOSED THIS CONNECTION]");
                                break;
                            }
                        
                            Console.Write(serverMessage + "\n");
                        }
                    }
                    catch (Exception ex)
                    {

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

        /// <summary>
        /// Sends a text string to the connected server.
        /// </summary>
        /// <param name="message">The message string to send.</param>
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

        /// <summary>
        /// Handles the process of uploading an image file to the server.
        /// Includes validating the file extension, checking file existence, verifying it is an image, 
        /// sending the header information, and streaming the raw bytes.
        /// </summary>
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

        /// <summary>
        /// Prompts the user to enter a valid file path for the image to be uploaded.
        /// </summary>
        /// <param name="fileName">The expected file name.</param>
        /// <returns>The full validated path to the file, or "exit" if the user cancels.</returns>
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


                if (File.Exists(fullPath))
                {
                    fileExists = true;
                }
                if (File.Exists(Path.Combine(fullPath, fileName)))
                {
                    fullPath = Path.Combine(fullPath, fileName);
                    fileExists = true;
                }

            }

            return fullPath;
        }

        /// <summary>
        /// Validates if the file at the specified path is a valid image format supported by System.Drawing.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file is a valid image; otherwise, false.</returns>
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

        /// <summary>
        /// Handles the process of downloading an image from the server.
        /// Pauses the main read cycle, requests the file list, prompts user for selection,
        /// reads the binary data, and save it to the selected folder on the local storage.
        /// </summary>
        private void DownloadImage()
        {
            pauseReadCylce = true;
            Console.WriteLine("Fetching image list from server...");

            string manifestRaw = WaitForMessage("FILE_LIST:", 5000);
            if (string.IsNullOrEmpty(manifestRaw))
            {
                Console.WriteLine("Failed to get list.");
                pauseReadCylce = false;
                return;
            }

            var fileMap = manifestRaw.Split('|')
                .Select(x => x.Split(':'))
                .ToDictionary(parts => parts[0], parts => long.Parse(parts[1]));

            Console.WriteLine("\n--- Available Images (Size in bytes) ---");
            foreach (var file in fileMap)
            {
                Console.WriteLine($"- {file.Key} ({file.Value} bytes)");
            }

            Console.Write("\nEnter file name to download: ");
            string selectedFile = Console.ReadLine().Trim();

            if (!fileMap.ContainsKey(selectedFile))
            {
                Console.WriteLine("Invalid file selection.");
                pauseReadCylce = false;
                return;
            }

            SendMessage(selectedFile);

            long sizeToRead = fileMap[selectedFile];

            try
            {
                Console.WriteLine($"Downloading {selectedFile}...");
                byte[] data = ReadBytesFromStream(client.GetStream(), sizeToRead);

                string savePath = PathExists();
                string fullPath = Path.Combine(savePath, selectedFile);
                File.WriteAllBytes(fullPath, data);
                Console.WriteLine("Success! Saved to: " + fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Download failed: " + ex.Message);
            }

            pauseReadCylce = false;
        }

        /// <summary>
        /// Waits for a specific message prefix from the server within a specified timeout.
        /// </summary>
        /// <param name="prefix">The message prefix to look for (e.g., "FILE_LIST:").</param>
        /// <param name="timeoutMs">The maximum time to wait in milliseconds.</param>
        /// <returns>The content of the message excluding the prefix, or null if timed out.</returns>
        private string WaitForMessage(string prefix, int timeoutMs)
        {
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                if (lastMessage != null && lastMessage.Contains(prefix))
                {
                    string content = lastMessage.Substring(lastMessage.IndexOf(prefix) + prefix.Length);
                    lastMessage = null;

                    return content;
                }
                Thread.Sleep(100);
                elapsed += 100;
            }
            return null;
        }

        /// <summary>
        /// Reads a specific number of bytes from the network stream into a memory buffer.
        /// </summary>
        /// <param name="stream">The network stream to read from.</param>
        /// <param name="count">The exact number of bytes to read.</param>
        /// <returns>A byte array containing the read data.</returns>
        /// <exception cref="IOException">Thrown if the connection is lost or data is incomplete.</exception>
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
                    Thread.Sleep(100);
                }

                if (bytesRemaining != 0)
                {
                    throw new IOException("Connection lost, or data was incomplete.");
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Prompts the user to enter a directory path to save the downloaded file.
        /// </summary>
        /// <returns>A valid directory path.</returns>
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


