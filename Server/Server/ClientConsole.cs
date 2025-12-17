using Server.Commands;

namespace Server
{
    /// <summary>
    /// Responsible for interpreting and executing commands sent by the connected client.
    /// It acts as the command processor (Invoker), mapping text inputs to specific ICommand implementations.
    /// </summary>
    public class ClientConsole
    {
        private bool isRunning = true;
        private Dictionary<string, ICommand> commands;
        private ClientHandler clientHandler;

        /// <summary>
        /// Initializes a new instance of the ClientConsole class.
        /// Configures the available commands and links them to the specific client handler.
        /// </summary>
        /// <param name="handler">The handler managing the network connection for this specific client.</param>
        public ClientConsole(ClientHandler handler)
        {
            clientHandler = handler;
            commands = new Dictionary<string, ICommand>
            {
                { "help", new Help() },
                { "exit", new Exit() },
                { "uploadimage", new UploadImage(clientHandler) },
                { "downloadimage", new DownloadImage(clientHandler) },
                { "listimage", new ListImage(clientHandler) }
            };
        }

        /// <summary>
        /// Reads a single line of input from the client, normalizes it, identifies the matching command, and executes it.
        /// If the command is valid, the result is sent back to the client.
        /// If the command is unknown, an error message is returned.
        /// </summary>
        private void Do()
        {
            string commandInput = clientHandler.Reader.ReadLine();
            try
            {
                commandInput = commandInput.ToLower();
                commandInput = commandInput.Trim();

                if (commands.ContainsKey(commandInput))
                {
                    clientHandler.Writer.WriteLine("<< " + commands[commandInput].Execute());
                    isRunning = !commands[commandInput].Exit();
                }
                else
                {
                    clientHandler.Writer.WriteLine($"<< Unknown command. Type 'help' for a list of commands. Your input- {commandInput}");

                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// Starts the main command processing loop.
        /// Continuously listens for client input until the "exit" command is received or the connection is lost.
        /// Performs cleanup (removing client from server, closing connections) when the loop ends.
        /// </summary>
        public void Start()
        {
            try
            {
                do
                {
                    Do();
                } while (isRunning);

                clientHandler.Stop();
                clientHandler.Server.RemoveClient(clientHandler);
            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Signals the processing loop to terminate gracefully by setting the running flag to false.
        /// </summary>
        public void Stop()
        {
            isRunning = false;
        }

    }
}
