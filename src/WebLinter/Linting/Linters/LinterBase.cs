// Modifications Copyright Rich Newman 2017
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
    public abstract class LinterBase
    {
        public LinterBase(ISettings settings, bool fixErrors)
        {
            Settings = settings;
            FixErrors = fixErrors;
        }

        public static NodeServer Server { get; } = new NodeServer();

        public string Name { get; set; }

        protected virtual string ConfigFileName { get; set; }

        protected virtual bool IsEnabled { get; set; }

        protected ISettings Settings { get; }
        protected bool FixErrors { get; }

        protected LintingResult Result { get; private set; }

        public async Task<LintingResult> Run(bool callSync, params string[] files)
        {
            Result = new LintingResult(files);

            if (!IsEnabled || !files.Any())
                return Result;

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

        protected virtual async Task<LintingResult> Lint(bool callSync, params FileInfo[] files)
        {
            string output = null;

            if(Settings.UseProjectNGLint)
            {
                try
                {
                    output = await RunLocalProcess(callSync, files);
                }
                catch (Exception)
                {
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


        protected async Task<string> RunLocalProcess(bool callSync, params FileInfo[] files)
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
            callSync = true;
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

                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed. Check process.ExitCode here.
                        int test = process.ExitCode;
                        stdErr = error.ToString();
                        stdOut = output.ToString();
                    }
                    else
                        throw new Exception("ng lint call on build timed out.  Timeout is 10 seconds.");
                }
            }
            return stdOut;
        }

        protected async Task<string> RunProcess(bool callSync, params FileInfo[] files)
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

        protected virtual string FindWorkingDirectory(FileInfo file)
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

        protected abstract void ParseErrors(string output);

        public override bool Equals(Object obj)
        {
            LinterBase lb = obj as LinterBase;
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
