// Modifications Copyright Rich Newman 2017
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string output = await RunProcess(callSync, files);

            if (!string.IsNullOrEmpty(output))
            {
                ParseErrors(output);
            }

            return Result;
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
