namespace Server.Commands
{
    /// <summary>
    /// Represents the "help" command.
    /// Provides the user with a list of all valid commands available in the server application.
    /// </summary>
    public class Help : ICommand
    {
        /// <summary>
        /// Returns a formatted string containing all supported commands.
        /// </summary>
        /// <returns>A string listing commands like help, exit, downloadImage, etc.</returns>
        public string Execute()
        {
            return "Available commands: help, exit, downloadImage, uploadImage, listImage";
        }

        /// <summary>
        /// Indicates that the session should continue after this command.
        /// </summary>
        /// <returns>Always returns false.</returns>
        public bool Exit()
        {
            return false;
        }
    }
}
