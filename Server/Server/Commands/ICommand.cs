namespace Server.Commands
{
    /// <summary>
    /// Defines a template for all executable commands in the server application.
    /// </summary>
    public interface ICommand
    {

        /// <summary>
        /// Executes the specific logic associated with the command.
        /// </summary>
        /// <returns>A string response containing the result of the execution, which is sent back to the client.</returns>
        public string Execute();

        /// <summary>
        /// Determines whether the client's session loop should terminate after this command is executed.
        /// </summary>
        /// <returns>True if the application should disconnect or stop the loop (e.g., "exit" command); otherwise, false.</returns>
        public bool Exit();
    }
}
