namespace TcpClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ImageClient client = new ImageClient();
            client.Connect();

        }
    }
}
