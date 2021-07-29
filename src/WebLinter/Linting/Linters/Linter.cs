using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebLinter
{
    public class Linter
    {
        public Linter(ISettings settings, bool fixErrors = false, Action<string, bool> log = null)
        {
            _settings = settings;
            _fixErrors = fixErrors;
            _log = log;
            if (settings.UseProjectNGLint) _localNgLintRunner = new LocalNgLintRunner(log);
        }

        public static NodeServer Server { get; } = new NodeServer();
        private LocalNgLintRunner _localNgLintRunner;
        private Action<string, bool> _log;

        public readonly string Name = "TSLint";
        public readonly string ConfigFileName = "tslint.json";
        private ISettings _settings { get; }
        private bool _fixErrors { get; }

        private LintingResult _result { get; set; }

        public async Task<LintingResult> Lint(bool callSync, params string[] files)
        {
            _result = new LintingResult(files);
            if (!_settings.TSLintEnable || !files.Any()) return _result;
            ServerPostData postData = CreatePostData(files);
            string output = _settings.UseProjectNGLint ? await _localNgLintRunner.Run(Name, postData, callSync) :
                                                         await Server.CallServer(Name, postData, callSync, _log);
            if (!string.IsNullOrEmpty(output)) ParseErrors(output, isCalledFromBuild: callSync);
            return _result;
        }

        private ServerPostData CreatePostData(string[] files)
        {
            ServerPostData postData = new ServerPostData
            {
                Config = Path.Combine(FindWorkingDirectory(files[0]), ConfigFileName),
                Files = files,
                FixErrors = _fixErrors,
                UseTSConfig = _settings.UseTsConfig
            };
#if DEBUG
            postData.Debug = true;
#endif
            return postData;
        }

        private string FindWorkingDirectory(string filePath)
        {
            DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(filePath));

            while (dir != null)
            {
                string rc = Path.Combine(dir.FullName, ConfigFileName);
                if (File.Exists(rc))
                    return dir.FullName;

                dir = dir.Parent;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private void ParseErrors(string output, bool isCalledFromBuild)
        {
            JArray array;
            try
            {
                array = JArray.Parse(output);
            }
            catch (JsonReaderException)
            {
                // In case of error from TSLint, just show error to user.
                // There is no use in showing exception details or throwing a new exception.
                LintingError le = new LintingError("TSLint", 0, 0, true, "TSLint")
                {
                    Message = output,
                    Provider = this
                };
                _result.Errors.Add(le);
                _result.HasVsErrors = true;
                return;
            }
            bool hasVSErrors = false;
            foreach (JObject obj in array)
            {
                string fileName = obj["name"]?.Value<string>().Replace("/", "\\");  // This is Windows, slashes are backwards
                if (string.IsNullOrEmpty(fileName)) continue;

                int lineNumber = obj["startPosition"]?["line"]?.Value<int>() ?? 0;
                int columnNumber = obj["startPosition"]?["character"]?.Value<int>() ?? 0;
                bool adjustForByteOrderMark = lineNumber == 0 && !_settings.UseTsConfig && HasUTF8ByteOrderMark(fileName);
                if (lineNumber == 0 && columnNumber > 0 && adjustForByteOrderMark) columnNumber--;  // Fix tslint off by one error
                bool isError = _settings.TSLintShowErrors ?
                    obj["ruleSeverity"]?.Value<string>()?.ToUpper() == "ERROR" : false;
                hasVSErrors = hasVSErrors || isError;
                string errorCode = obj["ruleName"]?.Value<string>();

                LintingError le = new LintingError(fileName, lineNumber, columnNumber, isError, errorCode);
                if (!_result.Errors.Contains(le))
                {
                    le.Message = obj["failure"]?.Value<string>();
                    le.EndLineNumber = obj["endPosition"]?["line"]?.Value<int>() ?? 0;
                    le.EndColumnNumber = obj["endPosition"]?["character"]?.Value<int>() ?? 0;
                    if (le.EndLineNumber == 0 && le.EndColumnNumber > 0 && adjustForByteOrderMark) le.EndColumnNumber--;

                    le.HelpLink = ParseHttpReference(le.Message, "https://goo.gl/") ??
                                  ParseHttpReference(le.Message, "https://angular.io/") ??
                                  $"https://palantir.github.io/tslint/rules/{le.ErrorCode}";
                    le.Provider = this;
                    le.IsBuildError = isCalledFromBuild;
                    _result.Errors.Add(le);
                }
            }
            _result.HasVsErrors = hasVSErrors;
        }

        // If you create a file in a Node console app it's encoded as UTF8 with a BOM
        // If you create a file in an ASP.NET app it's encoded as UTF8, no BOM
        // This doesn't usually matter. However, tslint treats the BOM as an extra character in the first line when calculating
        // StartColumnNumber and EndColumnNumber, which means we need to adjust for it to get our underlining positioning correct.
        // To clarify, you can have two apparently identical .ts files where tslint reports different column numbers for the same error
        // on the first line.  Fortunately we don't often have errors on the first line, and will only call this method if we do.
        // To make this even more difficult, tslint only does this if we're NOT using tsconfig.jsons as far as I can see.
        public static bool HasUTF8ByteOrderMark(string fileName)
        {
            var bom = new byte[3];
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 3);
            }
            return bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf;
        }

        private string ParseHttpReference(string message, string root)
        {
            int rootPosition = message == null ? -1 : message.LastIndexOf("(" + root);
            if (rootPosition == -1) return null;
            int bracePosition = message.LastIndexOf(")");
            if (bracePosition == -1 || bracePosition < rootPosition) return null;
            return message.Substring(rootPosition + 1, bracePosition - rootPosition - 1);
        }

        public override bool Equals(object obj)
        {
            Linter lb = obj as Linter;
            if (lb == null)
                return false;
            else
                return Name.Equals(lb.Name);
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString() => Name;

        public static bool IsLintableFileExtension(string fileName, bool lintJsFiles = true)
        {
            string extension = Path.GetExtension(fileName).ToUpperInvariant();
            return extension == ".TS" || extension == ".TSX" || (lintJsFiles && (extension == ".JS" || extension == ".JSX"));
        }
    }
}
