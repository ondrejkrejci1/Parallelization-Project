namespace Server
{
    /// <summary>
    /// Provides a thread-safe logging mechanism for the server application.
    /// It handles writing messages to a text file with timestamping, ensuring that concurrent 
    /// writes from multiple threads do not corrupt the log file.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// The relative file path where log entries are stored. 
        /// Default file name is "serverLog.txt".
        /// </summary>
        private const string logFilePath = "serverLog.txt";

        /// <summary>
        /// A synchronization object used to lock the file writing block, 
        /// preventing race conditions when multiple threads attempt to log simultaneously.
        /// </summary>
        private object lockObj = new object();

        /// <summary>
        /// Appends a new entry to the log file.
        /// The message is automatically prefixed with the current date and time.
        /// This method blocks other threads while writing to ensure data integrity.
        /// </summary>
        /// <param name="message">The text message to be recorded in the log.</param>
        public void Log(string message)
        {
            message = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} - {message}";

            lock (lockObj)
            {
                File.AppendAllText(logFilePath, message + Environment.NewLine);
            }
        }

    }
}
