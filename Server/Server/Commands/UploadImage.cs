using System.Net.Sockets;

namespace Server.Commands
{
    public class UploadImage : ICommand
    {
        private ClientHandler clientHandler;
        private const string UploadDirectory = "UploadedImages";

        public UploadImage(ClientHandler client)
        {
            this.clientHandler = client;
        }

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
                }

                if (bytesRemaining != 0)
                {
                    throw new IOException("Connection lost, or data was incomplete.");
                }

                return ms.ToArray();
            }
        }


        public bool Exit()
        {
            return false;
        }
    }
}
