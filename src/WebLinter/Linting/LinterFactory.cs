﻿// Modifications Copyright Rich Newman 2017
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebLinter
{
    public static class LinterFactory
    {
        public static readonly string ExecutionPath = Path.Combine(Path.GetTempPath(), Constants.CACHE_NAME + Constants.VERSION);
        private static AsyncLock _mutex = new AsyncLock();

        public static bool IsLintableFileExtension(string fileName, bool lintJsFiles)
        {
            string extension = Path.GetExtension(fileName).ToUpperInvariant();
            return extension == ".TS" || extension == ".TSX" || (lintJsFiles && (extension == ".JS" || extension == ".JSX"));
        }

        public static async Task<LintingResult[]> Lint(ISettings settings, params string[] fileNames) 
            => await Lint(settings, false, false, null, fileNames);

        public static async Task<LintingResult[]> Lint(ISettings settings, bool fixErrors, bool callSync, params string[] fileNames) 
            => await Lint(settings, fixErrors, callSync, null, fileNames);

        public static async Task<LintingResult[]> Lint(ISettings settings, bool fixErrors, bool callSync, Action<string, bool> log, params string[] fileNames)
        {
            if (fileNames.Length == 0)  return new LintingResult[0];

            await EnsureNodeFolderCreated(callSync);

            // TODO - do we even need the grouping???  Don't think so
            // Also the logic for selecting a linter is in here even though we now only have one
            var groupedFiles = fileNames.GroupBy(f => Path.GetExtension(f).ToUpperInvariant());
            Dictionary<Linter, IEnumerable<string>> dic = new Dictionary<Linter, IEnumerable<string>>();

            foreach (IGrouping<string, string> group in groupedFiles)
            {
                switch (group.Key)
                {
                    case ".TS":
                    case ".TSX":
                    case ".JSON":
                    case ".JS":
                    case ".JSX":
                        AddLinter(dic, new Linter(settings, fixErrors, log), group);
                        break;
                }
            }

            if (dic.Count != 0)
                return await Task.WhenAll(dic.Select(group => group.Key.Lint(callSync, group.Value.ToArray())));

            return new LintingResult[0];
        }

        private static void AddLinter(Dictionary<Linter, IEnumerable<string>> dic, Linter linter, IEnumerable<string> files)
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
        public static async Task EnsureNodeFolderCreated(bool callSync = false)
        {
            using (await _mutex.Lock(callSync))
            {
                var node_modules = Path.Combine(ExecutionPath, "node_modules");
                var log_file = Path.Combine(ExecutionPath, "log.txt");

                if (!Directory.Exists(node_modules) || !File.Exists(log_file) ||
                    (Directory.Exists(node_modules) && Directory.GetDirectories(node_modules).Length < 39))
                {
                    // This is called async at startup.  If we do a build before it's completed it gets
                    // called sync but the mutex will block the call until the previous async call has
                    // completed, so we shouldn't get here: we should be set up correctly.
                    // If we do a build  after someone's deleted a file, then we'll fail here.
                    if (callSync) throw new Exception("Node set up not valid on sync call, TSLint not run");

                    if (Directory.Exists(ExecutionPath))
                        Directory.Delete(ExecutionPath, recursive: true);

                    Directory.CreateDirectory(ExecutionPath);

                    var tasks = new List<Task>
                    {
                        SaveResourceFileAsync(ExecutionPath, "WebLinter.Node.node_modules.7z", "node_modules.7z"),
                        SaveResourceFileAsync(ExecutionPath, "WebLinter.Node.7z.exe", "7z.exe"),
                        SaveResourceFileAsync(ExecutionPath, "WebLinter.Node.7z.dll", "7z.dll"),
                        SaveResourceFileAsync(ExecutionPath, "WebLinter.Node.prepare.cmd", "prepare.cmd"),
                        SaveResourceFileAsync(ExecutionPath, "WebLinter.Node.server.js", "server.js"),
                        SaveResourceFileAsync(ExecutionPath, "WebLinter.Node.node.7z", "node.7z"),
                    };

                    await Task.WhenAll(tasks.ToArray());

                    ProcessStartInfo start = new ProcessStartInfo
                    {
                        WorkingDirectory = ExecutionPath,
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
