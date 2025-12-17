using System.Net.Sockets;

namespace Server.Commands
{
    /// <summary>
    /// Handles the "uploadimage" command, allowing a client to transfer an image file to the server.
    /// This class manages the handshake (filename/size negotiation), receives the raw binary data,
    /// saves the file to the local disk, and registers it in the server's list.
    /// </summary>
    public class UploadImage : ICommand
    {
        private ClientHandler clientHandler;

        /// <summary>
        /// The local directory where uploaded images will be saved.
        /// </summary>
        private const string UploadDirectory = "UploadedImages";

        /// <summary>
        /// Initializes a new instance of the UploadImage class.
        /// </summary>
        /// <param name="client">The handler for the connected client initiating the upload.</param>
        public UploadImage(ClientHandler client)
        {
            this.clientHandler = client;
        }

        /// <summary>
        /// Executes the server-side logic for the image upload protocol.
        /// 1. Prompts the client for metadata (Filename and Size).
        /// 2. Validates the input format.
        /// 3. Reads the exact number of bytes from the network stream.
        /// 4. Saves the data to the "UploadedImages" directory.
        /// 5. Registers the new file with the Server.
        /// </summary>
        /// <returns>A status message indicating success or a specific error (e.g., format mismatch, transfer incomplete).</returns>
        public string Execute()
        {
            
            clientHandler.Writer.WriteLine("Enter your image specification in format:\n>> <filename> <size_in_bytes>\n>> ");

            string[] specification = clientHandler.Reader.ReadLine().Split(' ', 2);

            if (specification.Length != 2 || !long.TryParse(specification[1], out long expectedSize))
            {
                return "ERROR: Invalid specification format. Expected: <filename> <size_in_bytes>";
            }

            string fileName = specification[0];
            string fullPath = Path.Combine(UploadDirectory, fileName);

            try
            {
                byte[] imageData = ReadBytesFromStream(clientHandler.Client.GetStream(), expectedSize);

                if (imageData.Length != expectedSize)
                {
                    return "ERROR: Transfer incomplete. Expected size mismatch.";
                }

                File.WriteAllBytes(fullPath,imageData);

            } 
            catch (Exception ex)
            {
                return $"ERROR: Failed to read image data. {ex.Message}";
            }

            clientHandler.Server.RegisterImage(fileName);

            return "SUCCESS: Image '{fileName}' ({expectedSize} bytes) uploaded and registered.";
        }

        /// <summary>
        /// Helper method to read a specific amount of bytes from the network stream.
        /// Ensures that all expected data is read.
        /// </summary>
        /// <param name="stream">The network stream to read from.</param>
        /// <param name="count">The exact number of bytes to read.</param>
        /// <returns>A byte array containing the read data.</returns>
        /// <exception cref="IOException">Thrown if the connection is lost or the stream ends before reading the expected amount of data.</exception>
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

        /// <summary>
        /// Indicates that the client session should remain active after the upload is complete.
        /// </summary>
        /// <returns>Always returns false.</returns>
        public bool Exit()
        {
            return false;
        }
    }
}
