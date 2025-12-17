namespace Server.Commands
{
    /// <summary>
    /// Represents the command responsible for retrieving and displaying the list of all 
    /// registered images available on the server to the client.
    /// </summary>
    public class ListImage : ICommand
    {
        private ClientHandler clientHandler;

        /// <summary>
        /// Initializes a new instance of the ListImage class.
        /// </summary>
        /// <param name="client">The client handler context, used to access the server's image registry.</param>
        public ListImage(ClientHandler client)
        {
            this.clientHandler = client;
        }

        /// <summary>
        /// Fetches the list of images from the server and formats them into a readable string.
        /// </summary>
        /// <returns>
        /// A string listing all registered image filenames. 
        /// Returns "No images found." if the server has no images.
        /// </returns>
        public string Execute()
        {
            List<string> images = clientHandler.Server.GetRegisteredImages();

            if (images.Count == 0)
            {
                return "No images found.";
            }

            string result = "Registered images:\n";

            foreach (string image in images)
            {
                result += $"- {image}\n";
            }

            return result;
        }

        /// <summary>
        /// Indicates that the client session should remain active after listing images.
        /// </summary>
        /// <returns>Always returns false.</returns>
        public bool Exit()
        {
            return false;
        }
    }
}
