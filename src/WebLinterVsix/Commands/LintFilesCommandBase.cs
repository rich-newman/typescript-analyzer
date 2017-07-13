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
            UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];
            IEnumerable<string> files = WebLinterPackage.Settings.UseTsConfig ?
                TsconfigLocations.FindPathsFromSelectedItems(selectedItems, WebLinterPackage.Dte.Solution) : 
                GetFilePathsFromSelectedItemPaths(selectedItems);
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
            IEnumerable<string> files = WebLinterPackage.Settings.UseTsConfig ? GetTsconfigBuildFilesToLint(isBuildingSolution) : 
                GetBuildFilesToLint(isBuildingSolution);
            if (!files.Any()) return false;
            return await LinterService.Lint(showErrorList: true, fixErrors: false, callSync: true, fileNames: files.ToArray());
        }

        private static IEnumerable<string> GetTsconfigBuildFilesToLint(bool isBuildingSolution)
        {
            if (isBuildingSolution)
            {
                UIHierarchyItem solutionUIHierarchyItem = GetUIHierarchySolutionItem();
                IEnumerable<string> paths = TsconfigLocations.FindPathsFromSelectedItems(new UIHierarchyItem[] { solutionUIHierarchyItem }, 
                                                                                            WebLinterPackage.Dte.Solution);
                return paths;
            }
            else
            {
                IEnumerable<UIHierarchyItem> projectHierarchyItems = ProjectHelpers.GetSelectedItemProjectUIHierarchyItems();
                IEnumerable<string> paths = TsconfigLocations.FindPathsFromSelectedItems(projectHierarchyItems.ToArray(),
                                                                                            WebLinterPackage.Dte.Solution);
                return paths;

            }
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

        private static UIHierarchyItem GetUIHierarchySolutionItem()
        {
            UIHierarchyItems uiHierarchyItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems;

            foreach (UIHierarchyItem item in uiHierarchyItems)
            {
                var test = item.Name;
                if (item.Object is Solution) return item;
            }
            return null;
        }

        //private static Dictionary<string, UIHierarchyItem> 

        // TODO make an enumerable: we cast to array immediately after this is called, so we're iterating multiple times for no reason
        private static List<string> GetFilePathsFromSelectedItemPaths(UIHierarchyItem[] selectedItems)
        {
            var paths = ProjectHelpers.GetSelectedItemPaths(selectedItems);
            List<string> files = new List<string>();
            foreach (string path in paths)
                AddFilesInPath(path, files);
            return files;
        }

        //private static IEnumerable<string> GetTsconfigFilePathsFromSelectedItemPaths(UIHierarchyItem[] selectedItems)
        //{
        //    return TsconfigLocations.FindPathsFromSelectedItems(selectedItems, WebLinterPackage.Dte.Solution);
        //}

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