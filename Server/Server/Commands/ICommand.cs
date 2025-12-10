namespace Server.Commands
{
    public interface ICommand
    {
        public string Execute();

        public bool Exit();
    }
}
