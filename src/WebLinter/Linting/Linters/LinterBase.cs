// Modifications Copyright Rich Newman 2017
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebLinter
{
    public class Linter
    {

        public Linter(ISettings settings, bool fixErrors, Action<string> log)
        {
            Settings = settings;
            FixErrors = fixErrors;
            this.log = log;
        }

        public static NodeServer Server { get; } = new NodeServer();

        public string Name { get; } = "TSLint";
        private string ConfigFileName { get; } = "tslint.json";
        private ISettings Settings { get; }
        private bool FixErrors { get; }
        private Action<string> log;

        private LintingResult Result { get; set; }

        private void CallLog(string message)
        {
            try
            {
                log?.Invoke(message);
            }
            catch (Exception) { }
        }

        public async Task<LintingResult> Lint(bool callSync, params string[] files)
        {
            Result = new LintingResult(files);

            if (!Settings.TSLintEnable || !files.Any()) return Result;

            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);

                if (!fileInfo.Exists)
                {
                    Result.Errors.Add(new LintingError(fileInfo.FullName, 0, 0, true, "") { Message = "The file doesn't exist" });
                    return Result;
                }

                fileInfos.Add(fileInfo);
            }

            return await Lint(callSync, fileInfos.ToArray());
        }

        private async Task<LintingResult> Lint(bool callSync, params FileInfo[] files)
        {
            string output = null;

            if(Settings.UseProjectNGLint)
            {
                try
                {
                    output = await RunLocalProcess(callSync, files);
                }
                catch (Exception ex)
                {
                    string message = "Attempted to lint with local 'ng lint' and failed.  Falling back to regular lint call.  ";
                    message += $"Error message:\n{ex.Message}";
                    CallLog(message);
                    output = await RunProcess(callSync, files);
                }
            }
            else
            {
                output = await RunProcess(callSync, files);
            }
            
            
            if (!string.IsNullOrEmpty(output))
            {
                ParseErrors(output);
            }

            return Result;
        }


        private async Task<string> RunLocalProcess(bool callSync, params FileInfo[] files)
        {
            var ConfigFile = new FileInfo(Path.Combine(FindWorkingDirectory(files[0]), ConfigFileName).Replace("\\", "/"));
            var Files = files.Select(f => f.FullName.Replace("\\", "/"));
            var Fix = FixErrors;

            var StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"ng lint --type-check {(Fix ? "--fix " : "")} --format JSON \"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                WorkingDirectory = ConfigFile.DirectoryName,
                UseShellExecute = false
            };
            string stdOut = "";
            string stdErr = "";
            if (callSync)
            {
                // Actually I think the old process blocks the UI thread anyway
                // so we can use RunLocalProcessSync in the async case as well
                stdOut = RunLocalProcessSync(StartInfo, out stdErr);
            }
            else
            {
                Process localLintProcess = new Process { StartInfo = StartInfo, EnableRaisingEvents = true };
                localLintProcess.Start();
                stdOut = await Task.Run(() => { return localLintProcess.StandardOutput.ReadToEnd(); });
                stdErr = await Task.Run(() => { return localLintProcess.StandardError.ReadToEnd(); });
                localLintProcess.WaitForExit();
            }

            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                string message = $@"Unable to LOCAL ng lint. : config: { ConfigFile }, dir: { ConfigFile.DirectoryName }
tslint Output: {stdErr} ";
                throw new System.FormatException(message, new InvalidOperationException(stdErr));
            }

            return stdOut;
        }

        // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
        private string RunLocalProcessSync(ProcessStartInfo startInfo, out string stdErr)
        {
            string stdOut = stdErr = "";
            int timeout = 10000;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) => {
                        if (e.Data == null)
                            outputWaitHandle.Set();
                        else
                            output.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            errorWaitHandle.Set();
                        else
                            error.AppendLine(e.Data);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout))
                    {
                        stdErr = error.ToString();
                        stdOut = output.ToString();
                    }
                    else
                        throw new Exception("ng lint call on build timed out.  Timeout is 10 seconds.");
                }
            }
            return stdOut;
        }

        private async Task<string> RunProcess(bool callSync, params FileInfo[] files)
        {
            var postMessage = new ServerPostData
            {
                Config = Path.Combine(FindWorkingDirectory(files[0]), ConfigFileName).Replace("\\", "/"),
                Files = files.Select(f => f.FullName.Replace("\\", "/")),
                FixErrors = FixErrors,
                UseTSConfig = Settings.UseTsConfig
            };
#if DEBUG
            postMessage.Debug = true;
#endif
            return await Server.CallServer(Name, postMessage, callSync);
        }

        private string FindWorkingDirectory(FileInfo file)
        {
            var dir = file.Directory;

            while (dir != null)
            {
                string rc = Path.Combine(dir.FullName, ConfigFileName);
                if (File.Exists(rc))
                    return dir.FullName;

                dir = dir.Parent;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private void ParseErrors(string output)
        {
            JArray array;
            try
            {
                array = JArray.Parse(output);
            }
            catch (Exception ex)
            {
                string message = $@"Unable to parse output from tslint. List of linting errors is expected.
tslint Output: {output}
Parsing Exception: {ex.Message}";
                throw new System.FormatException(message, ex);
            }
            HashSet<LintingError> seen = new HashSet<LintingError>();
            bool hasVSErrors = false;
            foreach (JObject obj in array)
            {
                string fileName = obj["name"]?.Value<string>().Replace("/", "\\");
                if (string.IsNullOrEmpty(fileName)) continue;

                int lineNumber = obj["startPosition"]?["line"]?.Value<int>() ?? 0;
                int columnNumber = obj["startPosition"]?["character"]?.Value<int>() ?? 0;
                bool isError = Settings.TSLintShowErrors ?
                    obj["ruleSeverity"]?.Value<string>() == "ERROR" : false;
                hasVSErrors = hasVSErrors || isError;
                string errorCode = obj["ruleName"]?.Value<string>();

                LintingError le = new LintingError(fileName, lineNumber, columnNumber, isError, errorCode);
                if (!Result.Errors.Contains(le))
                {
                    le.Message = obj["failure"]?.Value<string>();
                    le.HelpLink = ParseHttpReference(le.Message, "https://goo.gl/") ??
                                  ParseHttpReference(le.Message, "https://angular.io/") ??
                                  $"https://palantir.github.io/tslint/rules/{le.ErrorCode}";
                    le.Provider = this;
                    Result.Errors.Add(le);
                    seen.Add(le);
                }
            }
            Result.HasVsErrors = hasVSErrors;
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

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
