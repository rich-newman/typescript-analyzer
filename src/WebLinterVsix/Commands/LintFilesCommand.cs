// Modifications Copyright Rich Newman 2017
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace WebLinterVsix
{
    internal sealed class LintFilesCommand : LintFilesCommandBase
    {
        private readonly Package _package;

        public static bool RunOnBuild = true;

        private LintFilesCommand(Package package): base(package)
        {
            _package = package ?? throw new ArgumentNullException("package");

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(PackageGuids.WebLinterCmdSet, PackageIds.LintFilesCommand);
                var menuItem = new OleMenuCommand(async (s, e) => { await LintSelectedFiles(false); }, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
            base.StartListeningForChanges();
        }

        // TODO detect if we're just cleaning
        public override int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            try
            {
                bool cancelBuild = false;
                System.Threading.Tasks.Task<bool> task = LintSelectedFiles(fixErrors: false, callSync: true);
                // If we've called sync correctly task should be completed here, if not we may not have results anyway
                bool completed = task.IsCompleted;  //task.Wait(10); 
                if (completed) cancelBuild = task.Result;
                pfCancelUpdate = cancelBuild ? 1 : 0;
                if (cancelBuild) WebLinterPackage.Dte.StatusBar.Text = "Build failed because of TSLint Errors";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return VSConstants.S_OK;
        }

        public static LintFilesCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => _package;
        public static void Initialize(Package package) => Instance = new LintFilesCommand(package);
    }
}
