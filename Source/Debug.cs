using System;
using System.Threading.Tasks;
using System.IO;

namespace Rain_World_GameSense
{
    internal static class Debug
    {
        private static readonly string LogFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Debug).Assembly.Location), "..", "debug.log"));

        public static async Task Log(string message)
        {
            string directory = Path.GetDirectoryName(LogFilePath);
            if (Directory.Exists(directory) && File.Exists(LogFilePath))
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }

        public static async Task Clear()
        {
            string directory = Path.GetDirectoryName(LogFilePath);
            if (Directory.Exists(directory) && File.Exists(LogFilePath))
            {
                using (var writer = new StreamWriter(LogFilePath, append: false))
                {
                    await writer.WriteAsync(string.Empty);
                }
            }
        } 
    }
}
