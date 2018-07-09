using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebLinter
{
    internal class LocalNgLintRunner
    {
        private Action<string> _log;
        internal LocalNgLintRunner(Action<string> log) {_log = log; }

        internal async Task<string> Run(string name, ServerPostData postData, bool callSync)
        {
            string output = null;
            try
            {
                output = await RunLocalProcess(callSync, postData);
                CallLog("Lint with local 'ng lint' succeeded");
            }
            catch (Exception ex)
            {
                string message = "Attempted to lint with local 'ng lint' and failed.  Falling back to regular lint call.  ";
                message += $"Error message:\n{ex.Message}";
                CallLog(message);
                output = await Linter.Server.CallServer(name, postData, callSync, _log);
            }
            return output;
        }

        private async Task<string> RunLocalProcess(bool callSync, ServerPostData postData)
        {
            FileInfo configFile = new FileInfo(postData.Config);
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"ng lint --type-check {(postData.FixErrors ? "--fix " : "")} --format JSON \"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                WorkingDirectory = configFile.DirectoryName,
                UseShellExecute = false
            };
            string stdOut = "";
            string stdErr = "";
            if (callSync)
            {
                // Actually I think the old process blocks the UI thread anyway
                // so we can use RunLocalProcessSync in the async case as well
                stdOut = RunLocalProcessSync(startInfo, out stdErr);
            }
            else
            {
                Process localLintProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                localLintProcess.Start();
                stdOut = await Task.Run(() => { return localLintProcess.StandardOutput.ReadToEnd(); });
                stdErr = await Task.Run(() => { return localLintProcess.StandardError.ReadToEnd(); });
                localLintProcess.WaitForExit();
            }

            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                string message = $@"Unable to LOCAL ng lint. : config: { configFile }, dir: { configFile.DirectoryName }
tslint Output: {stdErr} ";
                throw new FormatException(message, new InvalidOperationException(stdErr));
            }

            return stdOut;
        }

        // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
        private string RunLocalProcessSync(ProcessStartInfo startInfo, out string stdErr)
        {
            string stdOut = stdErr = "";
            int timeout = 120000;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) => {
                        try
                        {
                            if (e.Data == null)
                                outputWaitHandle.Set();
                            else
                                output.AppendLine(e.Data);
                        }
                        catch (Exception) { }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        try
                        {
                            if (e.Data == null)
                                errorWaitHandle.Set();
                            else
                                error.AppendLine(e.Data);
                        }
                        catch (Exception) { }
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
                        throw new Exception("Local ng lint call on build timed out.  Timeout is 120 seconds.");
                }
            }
            return stdOut;
        }

        internal void CallLog(string message)
        {
            try
            {
                _log?.Invoke(message);
            }
            catch (Exception) { }
        }
    }
}
