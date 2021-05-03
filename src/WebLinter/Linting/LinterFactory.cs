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
            Linter linter = new Linter(settings, fixErrors, log);
            LintingResult result = await linter.Lint(callSync, fileNames);
            return new LintingResult[] { result };
        }
    }
}
