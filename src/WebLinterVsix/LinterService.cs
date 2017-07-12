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

        public static bool IsFileSupported(string fileName)
        {
            // Check if filename is absolute because when debugging, script files are sometimes dynamically created.
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName))
                return false;

            if (!LinterFactory.IsFileSupported(fileName))
                return false;

            string extension = Path.GetExtension(fileName);

            var patterns = WebLinterPackage.Settings.GetIgnorePatterns();

            if (patterns.Any(p => fileName.Contains(p)))
                return false;

            // Ignore nested files - if we're ignoring nested files we additionally return false if 
            // the item isn't in the solution or it has a parent project item that's a physical file (i.e. it's nested)
            // TODO We can actually get in here if an item isn't in the solution in general so maybe that first test
            // should be done whether we're ignoring nested files or not (else we have different behavior)
            if (WebLinterPackage.Settings.IgnoreNestedFiles && WebLinterPackage.Dte.Solution != null)
            {
                var item = WebLinterPackage.Dte.Solution.FindProjectItem(fileName);
                if (item == null) return false;

                // item.Collection is not supported in Node.js projects
                if (item.ContainingProject.Kind.Equals("{9092aa53-fb77-4645-b42d-1ccca6bd08bd}", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (item.Collection?.Parent is ProjectItem parent && 
                    parent.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                    return false;
            }

            return true;
        }

        public static async Task<bool> Lint(bool showErrorList, bool fixErrors, bool callSync, 
                                            string[] fileNames, string[] filterFileNames = null)
        {
            bool hasVSErrors = false;
            try
            {
                WebLinterPackage.Dte.StatusBar.Text = "Analyzing...";
                WebLinterPackage.Dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationGeneral);

                await CopyResourceFilesToUserProfile(false, callSync);

                var result = await LinterFactory.Lint(WebLinterPackage.Settings, fixErrors, callSync, fileNames);

                if (result != null)
                {
                    ErrorListService.ProcessLintingResults(result, fileNames, filterFileNames, showErrorList);
                    hasVSErrors = result.Any(r => r.HasVsErrors);
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