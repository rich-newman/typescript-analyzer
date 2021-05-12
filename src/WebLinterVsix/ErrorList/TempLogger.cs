using System.Diagnostics;
using System.IO;

namespace WebLinterVsix.ErrorList
{
    /// <summary>
    /// Logger created to write to text files because of issues where builds once installed weren't fixing and underlining correctly
    /// </summary>
    class TempLogger
    {
        private static readonly bool disabled = true;  // Make false to turn it on
        private static readonly string filePath = @"C:\0log\templog.txt";
        static TempLogger()
        {
            if (disabled) return;
            CreateFile();
        }

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            if (disabled) return;
            if (!File.Exists(filePath)) CreateFile();
            string contents = File.ReadAllText(filePath);
            File.WriteAllText(filePath, contents + "\n" + message);
        }

        [Conditional("DEBUG")]
        private static void CreateFile()
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath))) Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.Delete(filePath);
            File.WriteAllText(filePath, "TEMPLOGGER\n");
        }
    }
}
