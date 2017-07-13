using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLinterVsix.Helpers;

namespace WebLinterVsix
{
    internal class LintFilesCommandBase: BuildEventsBase
    {
        internal LintFilesCommandBase(Package package) : base(package) { }

        protected void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = LinterService.AreAllSelectedItemsLintable();
        }

        protected async System.Threading.Tasks.Task<bool> LintSelectedFiles(bool fixErrors)
        {
            if (!LinterService.IsLinterEnabled)
            {
                WebLinterPackage.Dte.StatusBar.Text = "TSLint is not enabled in Tools/Options";
                return false;
            }
            IEnumerable<string> files = WebLinterPackage.Settings.UseTsConfig ? GetTsconfigFilesFromSelectedItemPaths() 
                : GetFilesInSelectedItemPaths();
            UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];
            string[] filterFileNames = TsconfigLocations.FindFilterFiles(selectedItems, WebLinterPackage.Dte.Solution);
            if (files.Any())
            {
                return await LinterService.Lint(showErrorList: true, fixErrors: fixErrors, 
                                                callSync: false, fileNames: files.ToArray(), filterFileNames: filterFileNames);
            }
            else
            {
                WebLinterPackage.Dte.StatusBar.Text = $"No files found to {(fixErrors ? "fix" : "lint")}";
                return false;
            }
        }

        protected async System.Threading.Tasks.Task<bool> LintBuildSelection(bool isBuildingSolution)
        {
            if (!LinterService.IsLinterEnabled) return false;
            List<string> files = GetBuildFilesToLint(isBuildingSolution);
            if (!files.Any()) return false;
            return await LinterService.Lint(showErrorList: true, fixErrors: false, callSync: true, fileNames: files.ToArray());
        }

        private static List<string> GetBuildFilesToLint(bool isBuildingSolution)
        {
            List<string> files = new List<string>();
            if (isBuildingSolution)
            {
                string path = ProjectHelpers.GetSolutionPath();
                AddFilesInPath(path, files);
            }
            else
            {
                var paths = ProjectHelpers.GetSelectedItemProjectPaths();
                foreach (string path in paths)
                    AddFilesInPath(path, files);
            }
            return files;
        }

        // TODO make an enumerable: we cast to array immediately after this is called, so we're iterating multiple times for no reason
        private static List<string> GetFilesInSelectedItemPaths()
        {
            var paths = ProjectHelpers.GetSelectedItemPaths();
            List<string> files = new List<string>();
            foreach (string path in paths)
                AddFilesInPath(path, files);
            return files;
        }

        private static IEnumerable<string> GetTsconfigFilesFromSelectedItemPaths()
        {
            UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];
            return TsconfigLocations.FindPathsFromSelectedItems(selectedItems, WebLinterPackage.Dte.Solution);
        }

        private static void AddFilesInPath(string path, List<string> files)
        {
            if (Directory.Exists(path))
            {
                var children = GetFiles(path, "*.*");
                files.AddRange(children.Where(c => LinterService.IsLintableTsOrTsxFile(c)));
            }
            else if (File.Exists(path) && LinterService.IsLintableTsOrTsxFile(path))
            {
                files.Add(path);
            }
        }

        private static List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
    }

}