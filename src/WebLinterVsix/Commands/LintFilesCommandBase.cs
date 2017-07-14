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
                ((OleMenuCommand)sender).Visible = LinterService.AreAllSelectedItemsLintable();
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        protected async System.Threading.Tasks.Task<bool> LintSelectedFiles(bool fixErrors)
        {
            try
            {
                if (!LinterService.IsLinterEnabled)
                {
                    WebLinterPackage.Dte.StatusBar.Text = "TSLint is not enabled in Tools/Options";
                    return false;
                }
                UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];
                IEnumerable<string> files = WebLinterPackage.Settings.UseTsConfig ?
                                            TsconfigLocations.FindPathsFromSelectedItems(selectedItems, WebLinterPackage.Dte.Solution) :
                                            LintFileLocations.GetFilePathsFromSelectedItemPaths(selectedItems);
                if (files.Any())
                {
                    string[] filterFileNames = WebLinterPackage.Settings.UseTsConfig ?
                                               TsconfigLocations.FindFilterFiles(selectedItems, WebLinterPackage.Dte.Solution) : null;
                    return await LinterService.Lint(showErrorList: true, fixErrors: fixErrors, callSync: false,
                                                    fileNames: files.ToArray(), filterFileNames: filterFileNames);
                }
                else
                {
                    WebLinterPackage.Dte.StatusBar.Text = $"No files found to {(fixErrors ? "fix" : "lint")}";
                    return false;
                }
        }
            catch (Exception ex)
            {
                Logger.LogAndWarn(ex);
                LinterBase.Server.Down();
                return false;
            }
}

        protected async System.Threading.Tasks.Task<bool> LintBuildSelection(bool isBuildingSolution)
        {
            try
            {
                if (!LinterService.IsLinterEnabled) return false;
                IEnumerable<string> files = WebLinterPackage.Settings.UseTsConfig ?
                                                BuildFileLocations.GetTsconfigBuildFilesToLint(isBuildingSolution) :
                                                BuildFileLocations.GetBuildFilesToLint(isBuildingSolution);
                if (!files.Any()) return false;
                return await LinterService.Lint(showErrorList: true, fixErrors: false, callSync: true, fileNames: files.ToArray());
            }
            catch (Exception ex)
            {
                Logger.LogAndWarn(ex);
                LinterBase.Server.Down();
                return false;
            }
        }

    }

}