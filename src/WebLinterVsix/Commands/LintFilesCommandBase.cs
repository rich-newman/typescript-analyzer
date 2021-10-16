using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLinter;
using WebLinterVsix.Helpers;

namespace WebLinterVsix
{
    internal class LintFilesCommandBase: BuildEventsBase
    {
        internal LintFilesCommandBase(Package package) : base(package) { }

        protected void BeforeQueryStatus(object sender, EventArgs e)
        {
            try
            {
                ((OleMenuCommand)sender).Visible = LintableFiles.AreAllSelectedItemsLintable();
            }
            catch (Exception ex)
            {
                Logger.LogAndWarn(ex);
            }
        }

        protected async System.Threading.Tasks.Task<bool> LintSelectedFiles(bool fixErrors)
        {
            try
            {
                Benchmark.Start();
                if (!LinterService.IsLinterEnabled)
                {
                    WebLinterPackage.Dte.StatusBar.Text = "TSLint is not enabled in Tools/Options";
                    return false;
                }
                UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];
                return await LintSelectedItems(fixErrors, selectedItems);
            }
            catch (Exception ex)
            {
                Logger.LogAndWarn(ex);
                Linter.Server.Down();
                return false;
            }
            finally { Benchmark.End(); }
        }

        internal static async System.Threading.Tasks.Task<bool> LintSelectedItems(bool fixErrors, UIHierarchyItem[] selectedItems)
        {
            Dictionary<string, string> fileToProjectMap;
            IEnumerable<string> files = WebLinterPackage.Settings.UseTsConfig ?
                                        TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out fileToProjectMap) :
                                        LintFileLocations.FindPathsFromSelectedItems(selectedItems, out fileToProjectMap);
            if (files.Any())
            {
                bool clearAllErrors = AnyItemNotLintableSingleFile(selectedItems);
                return await LinterService.Lint(showErrorList: true, fixErrors: fixErrors, callSync: false, fileNames: files.ToArray(),
                                                clearAllErrors, fileToProjectMap);
            }
            else
            {
                WebLinterPackage.Dte.StatusBar.Text = $"No {(WebLinterPackage.Settings.UseTsConfig ? "tsconfig.json" : "ts or tsx")}" +
                    $" files found to {(fixErrors ? "fix" : "lint")}";
                return false;
            }
        }

        private static bool AnyItemNotLintableSingleFile(UIHierarchyItem[] items)
        {
            foreach (UIHierarchyItem selItem in items)
            {
                if (!(selItem.Object is ProjectItem item &&
                    item.GetFullPath() is string projectItemPath &&
                    LintableFiles.IsLintableTsTsxJsJsxFile(projectItemPath)))
                    return true;
            }
            return false;
        }

        protected async System.Threading.Tasks.Task<bool> LintBuildSelection(bool isBuildingSolution)
        {
            try
            {
                Benchmark.Start();
                if (!LinterService.IsLinterEnabled) return false;
                UIHierarchyItem[] selectedItems = BuildSelectedItems.Get(isBuildingSolution);
                Dictionary<string, string> fileToProjectMap;
                string[] files = WebLinterPackage.Settings.UseTsConfig ?
                                            TsconfigLocations.FindPathsFromSelectedItems(selectedItems, out fileToProjectMap) :
                                            LintFileLocations.FindPathsFromSelectedItems(selectedItems, out fileToProjectMap);
                if (!files.Any()) return false;
                return await LinterService.Lint(showErrorList: true, fixErrors: false, callSync: true,
                                                fileNames: files, clearAllErrors: true, fileToProjectMap);
            }
            catch (Exception ex)
            {
                Logger.LogAndWarn(ex);
                Linter.Server.Down();
                return true;  // Return value is true if we have VS errors
            }
            finally { Benchmark.End(); }
        }

    }

}