// Modifications Copyright Rich Newman 2017
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebLinter
{
    public static class LinterFactory
    {
        private static string[] _supported = new string[] { ".TS", ".TSX" };
        private static object _syncRoot = new object();
        private static AsyncLock _mutex = new AsyncLock();

        public static bool IsFileSupported(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToUpperInvariant();

            return _supported.Contains(extension);
        }

        public static async Task<LintingResult[]> LintAsync(ISettings settings, params string[] fileNames)
        {
            return await LintAsync(settings, false, fileNames);
        }

        public static async Task<LintingResult[]> LintAsync(ISettings settings, bool fixErrors, params string[] fileNames)
        {
            if (fileNames.Length == 0)
                return new LintingResult[0];

            string extension = Path.GetExtension(fileNames[0]).ToUpperInvariant();
            var groupedFiles = fileNames.GroupBy(f => Path.GetExtension(f).ToUpperInvariant());
            Dictionary<LinterBase, IEnumerable<string>> dic = new Dictionary<LinterBase, IEnumerable<string>>();

            foreach (var group in groupedFiles)
            {
                switch (group.Key)
                {
                    case ".TS":
                    case ".TSX":
                        AddLinter(dic, new TsLintLinter(settings, fixErrors), group);
                        break;
                }
            }

            if (dic.Count != 0)
            {
                await InitializeAsync();

                return await Task.WhenAll(dic.Select(group => group.Key.Run(group.Value.ToArray())));
            }

            return new LintingResult[0];
        }

        private static void AddLinter(Dictionary<LinterBase, IEnumerable<string>> dic, LinterBase linter, IEnumerable<string> files)
        {
            if (dic.ContainsKey(linter))
            {
                dic[linter] = dic[linter].Union(files);
            }
            else
            {
                dic.Add(linter, files);
            }
        }

        /// <summary>
        /// Initializes the Node environment.
        /// </summary>
        public static async Task InitializeAsync()
        {
            using (await _mutex.LockAsync())
            {
                // TODO can be a property
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uriBuilder = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uriBuilder.Path);
                string executionPath = Path.GetDirectoryName(path);

                var edgePath = Path.Combine(executionPath, "edge");
                var node_modulesPath = Path.Combine(edgePath, "node_modules");
                var log_file = Path.Combine(executionPath, "log.txt");

                // TODO the directories check
                if (!Directory.Exists(edgePath) || !File.Exists(log_file) ||
                    (Directory.Exists(edgePath) && Directory.GetDirectories(node_modulesPath).Length < 18))
                {
                    if (Directory.Exists(edgePath))
                        Directory.Delete(edgePath, recursive: true);

                    var tasks = new List<Task>
                    {
                        SaveResourceFileAsync(executionPath, "WebLinter.Node.node_modules.7z", "node_modules.7z"),
                        SaveResourceFileAsync(executionPath, "WebLinter.Node.7z.exe", "7z.exe"),
                        SaveResourceFileAsync(executionPath, "WebLinter.Node.7z.dll", "7z.dll"),
                        SaveResourceFileAsync(executionPath, "WebLinter.Node.prepare.cmd", "prepare.cmd"),
                        SaveResourceFileAsync(executionPath, "WebLinter.Node.edge.7z", "edge.7z"),
                    };

                    await Task.WhenAll(tasks.ToArray());

                    ProcessStartInfo start = new ProcessStartInfo
                    {
                        WorkingDirectory = executionPath,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = "/c prepare.cmd"
                    };

                    Process p = Process.Start(start);
                    await p.WaitForExitAsync();

                    // If this file is written, then the initialization was successful.
                    using (var writer = new StreamWriter(log_file))
                    {
                        await writer.WriteAsync(DateTime.Now.ToLongDateString());
                    }
                }
            }
        }

        private static async Task SaveResourceFileAsync(string path, string resourceName, string fileName)
        {
            File.Delete(Path.Combine(path, fileName));
            using (Stream stream = typeof(LinterFactory).Assembly.GetManifestResourceStream(resourceName))
            using (FileStream fs = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await stream.CopyToAsync(fs);
            }
        }
    }
}
