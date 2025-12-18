namespace Server.Commands
{
    /// <summary>
    /// Represents the command used to change the display name of the connected client.
    /// It handles user input validation and notifies the server about the identity change.
    /// </summary>
    public class SetName : ICommand
    {
        private ClientHandler client;

        /// <summary>
        /// Initializes a new instance of the SetName class.
        /// </summary>
        /// <param name="client">The client handler instance whose name will be updated.</param>
        public SetName(ClientHandler client)
        {
            this.client = client;
        }

        /// <summary>
        /// Executes the name change logic.
        /// 1. Prompts the user to enter a new name.
        /// 2. Validates that the input is not empty or whitespace.
        /// 3. Updates the client's name and logs the change to the server console.
        /// </summary>
        /// <returns>A confirmation message with the new name.</returns>
        public string Execute()
        {
            string oldName = client.Name;

            client.Writer.WriteLine("<< Enter your name:\n>> ");

            while(true)
            {
                string name = client.Reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    client.SetName(name.Trim());
                    client.Server.PrintMessage($"Client {oldName} changed their name to: {client.Name}");
                    client.Server.Logger.Log($"Client {oldName} changed their name to: {client.Name}");
                    return $"Name set to: {client.Name}";
                }
                else
                {
                    client.Writer.WriteLine("<< Name cannot be empty. Please enter a valid name:\n>> ");
                }
            }

        }

        /// <summary>
        /// Indicates that the client session should continue after the name change.
        /// </summary>
        /// <returns>Always returns false.</returns>
        public bool Exit()
        {
            return false;
        }
    }
}
