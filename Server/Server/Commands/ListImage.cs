namespace Server.Commands
{
    public class ListImage : ICommand
    {
        private ClientHandler clientHandler;
        public ListImage(ClientHandler client)
        {
            this.clientHandler = client;
        }

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

        public bool Exit()
        {
            return false;
        }
    }
}
