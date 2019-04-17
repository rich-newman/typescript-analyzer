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
        public static readonly string ExecutionPath = Path.Combine(AssemblyDirectory, Constants.NODE_FOLDER_NAME); // + Constants.VERSION);

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

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
    }
}
