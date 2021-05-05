using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EnvDTE;
using WebLinter;

namespace WebLinterVsix
{
    internal static class LinterService
    {
        private static bool _defaultsCreated;

        public static bool IsLinterEnabled => WebLinterPackage.Settings.TSLintEnable;

         public static async Task<bool> Lint(bool showErrorList, bool fixErrors, bool callSync, 
                                            string[] fileNames, string[] filterFileNames = null)
        {
#if DEBUG
            if (fileNames.Length == 0) throw new Exception("LinterService/Lint called with empty fileNames list");
#endif
            bool hasVSErrors = false;
            try
            {
                WebLinterPackage.Dte.StatusBar.Text = "Analyzing...";
                WebLinterPackage.Dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationGeneral);

                await CopyResourceFilesToUserProfile(false, callSync);
                Linter linter = new Linter(WebLinterPackage.Settings, fixErrors, Logger.LogAndWarn);
                LintingResult result = await linter.Lint(callSync, fileNames);

                if (result != null)
                {
                    ErrorListService.ProcessLintingResults(result, fileNames, filterFileNames, showErrorList);
                    hasVSErrors = result.HasVsErrors;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                WebLinterPackage.Dte.StatusBar.Clear();
                WebLinterPackage.Dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationGeneral);
            }
            return hasVSErrors;
        }

        public static async Task CopyResourceFilesToUserProfile(bool force = false, bool callSync = false)
        {
            // Not sure about the defaultsCreated flag here: if you delete your own tslint.json whilst
            // VS is running we're going to fail until you restart
            if (!_defaultsCreated || force)
            {
                string sourceFolder = GetVsixFolder();
                string destFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                try
                {
                    foreach (string sourceFile in Directory.EnumerateFiles(sourceFolder))
                    {
                        string fileName = Path.GetFileName(sourceFile);
                        string destFile = Path.Combine(destFolder, fileName);

                        if (force || !File.Exists(destFile))
                        {
                            using (var source = File.Open(sourceFile, FileMode.Open))
                            using (var dest = File.Create(destFile))
                            {
                                if (callSync)
                                    source.CopyTo(dest);
                                else
                                    await source.CopyToAsync(dest);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

                _defaultsCreated = true;
            }
        }

        private static string GetVsixFolder()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string root = Path.GetDirectoryName(assembly);
            return Path.Combine(root, "Resources\\Defaults");
        }
    }
}