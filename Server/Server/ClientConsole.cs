using Server.Commands;

namespace Server
{
    public class ClientConsole
    {
        private bool isRunning = true;
        private Dictionary<string, ICommand> commands;
        private ClientHandler clientHandler;

        public ClientConsole(ClientHandler handler)
        {
            clientHandler = handler;
            commands = new Dictionary<string, ICommand>
            {
                { "help", new Help() },
                { "exit", new Exit() },
                { "uploadimage", new UploadImage() },
                { "downloadimage", new DownloadImage() },
                { "listimage", new ListImage() }
            };
        }

        private void Do()
        {
            clientHandler.Writer.Write(">> ");
            string commandInput = clientHandler.Reader.ReadLine();
            try
            {
                commandInput = commandInput.ToLower();
                commandInput = commandInput.Trim();

                if (commands.ContainsKey(commandInput))
                {
                    clientHandler.Writer.WriteLine(commands[commandInput].Execute());
                    isRunning = !commands[commandInput].Exit();
                }
                else
                {
                    clientHandler.Writer.WriteLine($"Unknown command. Type 'help' for a list of commands. Your input- {commandInput}");

                }
            } catch (NullReferenceException nullImput)
            {
                clientHandler.Writer.WriteLine("Connection closed due to invalid input.");
                isRunning = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public void Start()
        {
            try
            {
                do
                {
                    Do();
                } while (isRunning);

            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
