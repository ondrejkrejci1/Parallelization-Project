namespace Server.Commands
{
    public class Exit : ICommand
    {
        public string Execute()
        {
            return "Exiting the application. Goodbye!";
        }

        bool ICommand.Exit()
        {
            return true;
        }
    }
}
