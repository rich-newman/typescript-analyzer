// Modifications Copyright Rich Newman 2017
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
        public Linter(ISettings settings, bool fixErrors, Action<string, bool> log)
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

            List<FileInfo> fileInfos = new List<FileInfo>();
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (!fileInfo.Exists)
                {
                    _result.Errors.Add(new LintingError(fileInfo.FullName, 0, 0, true, "") { Message = "The file doesn't exist" });
                    return _result;
                }
                fileInfos.Add(fileInfo);
            }

            return await Lint(callSync, fileInfos.ToArray());
        }

        private async Task<LintingResult> Lint(bool callSync, params FileInfo[] files)
        {
            // The ng lint runner doesn't need the files list, does need tslint.json
            ServerPostData postData = CreatePostData(files);
            string output = _settings.UseProjectNGLint ? await _localNgLintRunner.Run(Name, postData, callSync) :
                                                         await Server.CallServer(Name, postData, callSync, _log);
            if (!string.IsNullOrEmpty(output)) ParseErrors(output, isCalledFromBuild: callSync);
            return _result;
        }

        private ServerPostData CreatePostData(FileInfo[] files)
        {
            ServerPostData postData = new ServerPostData
            {
                Config = Path.Combine(FindWorkingDirectory(files[0]), ConfigFileName).Replace("\\", "/"),
                Files = files.Select(f => f.FullName.Replace("\\", "/")),
                FixErrors = _fixErrors,
                UseTSConfig = _settings.UseTsConfig
            };
#if DEBUG
            postData.Debug = true;
#endif
            return postData;
        }

        private string FindWorkingDirectory(FileInfo file)
        {
            DirectoryInfo dir = file.Directory;

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
            JArray array = null;
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
                string fileName = obj["name"]?.Value<string>().Replace("/", "\\");
                if (string.IsNullOrEmpty(fileName)) continue;

                int lineNumber = obj["startPosition"]?["line"]?.Value<int>() ?? 0;
                int columnNumber = obj["startPosition"]?["character"]?.Value<int>() ?? 0;
                bool isError = _settings.TSLintShowErrors ?
                    obj["ruleSeverity"]?.Value<string>()?.ToUpper() == "ERROR" : false;
                hasVSErrors = hasVSErrors || isError;
                string errorCode = obj["ruleName"]?.Value<string>();

                LintingError le = new LintingError(fileName, lineNumber, columnNumber, isError, errorCode);
                if (!_result.Errors.Contains(le))
                {
                    le.Message = obj["failure"]?.Value<string>();
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
    }
}
