namespace Server.Commands
{
    /// <summary>
    /// Represents the command used to terminate the client's session.
    /// When executed, it sends a farewell message and signals the connection loop to stop.
    /// </summary>
    public class Exit : ICommand
    {
        /// <summary>
        /// Generates a farewell message to be sent to the client before disconnection.
        /// </summary>
        /// <returns>A string containing the goodbye message.</returns>
        public string Execute()
        {
            return "Exiting the application. Goodbye!";
        }

        /// <summary>
        /// Returns true to indicate that the client session should end.
        /// This signals the main loop in ClientConsole to break and close the connection.
        /// </summary>
        /// <returns>Always returns true.</returns>
        bool ICommand.Exit()
        {
            return true;
        }
    }
}
