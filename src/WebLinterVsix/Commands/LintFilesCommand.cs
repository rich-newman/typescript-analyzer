// Modifications Copyright Rich Newman 2017
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using I = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;

namespace WebLinterVsix
{
    enum BuildType
    {

    }
    internal sealed class LintFilesCommand : LintFilesCommandBase
    {
        private readonly Package _package;
        private readonly CommandEvents _commandEvents;

        private LintFilesCommand(Package package): base(package)
        {
            _package = package ?? throw new ArgumentNullException("package");
            _commandEvents = WebLinterPackage.Dte.Events.CommandEvents;
            _commandEvents.BeforeExecute += _commandEvents_BeforeExecute;

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(PackageGuids.WebLinterCmdSet, PackageIds.LintFilesCommand);
                var menuItem = new OleMenuCommand(async (s, e) => { await LintSelectedFiles(false); }, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
            base.StartListeningForChanges();
        }

        private bool _isBuilding = false;
        private bool _isBuildingSolution = true;
        private HashSet<int> _buildIds = new HashSet<int> { (int)I.BuildSln, (int)I.RebuildSln, (int)I.BuildCtx, (int)I.RebuildCtx,
                                                            (int)I.BuildSel, (int)I.RebuildSel, (int)I.BatchBuildDlg, (int)I.Start,
                                                            (int)I.StartNoDebug };
        private HashSet<int> _buildSolutionIds = new HashSet<int> { (int)I.BuildSln, (int)I.RebuildSln, (int)I.BatchBuildDlg,
                                                                    (int)I.Start, (int)I.StartNoDebug };
        private void _commandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            // TODO There's also Build1-9, Rebuild1-9 and BuildLast/RebuildLast.  
            // 1-9 can be called from the build menu if on an item outside a project.
            // These are really edge cases and a bit of work to implement as we need to map them to projects and tell the linter
            // TODO Batch build lints everything, not just the built projects
            if (!WebLinterPackage.Settings.RunOnBuild || !Guid.StartsWith("{5E") || Guid != "{5EFC7975-14BC-11CF-9B2B-00AA00573819}") return;
            if (!_buildIds.Contains(ID)) return;
            _isBuilding = true;
            _isBuildingSolution = _buildSolutionIds.Contains(ID);
        }

        public override int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            try
            {
                if (!_isBuilding || !WebLinterPackage.Settings.RunOnBuild) return VSConstants.S_OK;
                bool cancelBuild = false;
                System.Threading.Tasks.Task<bool> task = LintBuildSelection(_isBuildingSolution);
                // If we've called sync correctly task should be completed here, if not we may not have results anyway
                // Exceptions are in general swallowed and logged by the linter, which will return false here
                bool completed = task.IsCompleted;  //task.Wait(10);
                if (!completed) throw new Exception("Linting on build failed to complete correctly");
                cancelBuild = task.Result;
                pfCancelUpdate = cancelBuild ? 1 : 0;
                if (cancelBuild) WebLinterPackage.Dte.StatusBar.Text = "Build failed because of TSLint Errors";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                _isBuilding = false;
            }
            return VSConstants.S_OK;
        }

        public static LintFilesCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => _package;
        public static void Initialize(Package package) => Instance = new LintFilesCommand(package);
    }
}
