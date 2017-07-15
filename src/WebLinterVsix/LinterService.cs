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

        public static bool AreAllSelectedItemsLintable()
        {
            UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];

            foreach (UIHierarchyItem selectedItem in selectedItems)
            {
                if (!IsLintable(selectedItem)) return false;
            }
            return true;
        }

        public static bool IsLintable(UIHierarchyItem selectedItem)
        {
            return selectedItem.Object is Solution ||
                   selectedItem.Object is Project ||
                   selectedItem.Object is EnvDTE80.SolutionFolder ||
                  (selectedItem.Object is ProjectItem item &&
                        item.GetFullPath() is string projectItemPath &&
                        IsLintableProjectItem(projectItemPath));
        }


        public static bool IsLintableProjectItem(string projectItemPath)
        {
            return IsLintableDirectory(projectItemPath) || 
                   IsLintableTsOrTsxFile(projectItemPath) ||
                   (WebLinterPackage.Settings.UseTsConfig && IsLintableTsconfig(projectItemPath));
        }

        public static bool IsLintableDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;
            if (WebLinterPackage.Settings.GetIgnorePatterns().Any(p => path.Contains(p))) return false;
            // TODO Folder is not in project??  Below always returns null, so how do we check?
            ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(path);

            return true;
        }

        public static bool IsLintableTsOrTsxFile(string fileName)
        {
            // Check if filename is absolute because when debugging, script files are sometimes dynamically created.
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!LinterFactory.IsExtensionTsOrTsx(fileName)) return false;
            return IsLintableFile(fileName);
        }

        public static bool IsLintableTsconfig(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!fileName.EndsWith("tsconfig.json", ignoreCase: true, culture: null)) return false;
            return IsLintableFile(fileName);
        }

        private static bool IsLintableFile(string fileName)
        {
            if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.GetIgnorePatterns().Any(p => fileName.Contains(p)))
                return false;

            ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(fileName);
            bool isInProject = item?.GetFullPath() is string;
            if (!isInProject) return false;

            // Ignore nested files
            if (WebLinterPackage.Settings.IgnoreNestedFiles)
            {
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