namespace Server.Commands
{
    public class Help : ICommand
    {
        public string Execute()
        {
            return "Available commands: help, exit, downloadImage, uploadImage, listImage";
        }

        public bool Exit()
        {
            return false;
        }
    }
}
