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
            bool cancelBuild = false;
            System.Threading.Tasks.Task<bool> task = LintSelectedFiles(fixErrors: false, callSync: true);
            // We've called sync on the UI thread so the task should be complete when we return
            // Don't block the UI thread if it's not
            if (task.IsCompleted)
                cancelBuild = task.Result;
            pfCancelUpdate = cancelBuild ? 1 : 0;
            if(cancelBuild)
                WebLinterPackage.Dte.StatusBar.Text = "Build cancelled because of TSLint Errors";
            //else
            //    WebLinterPackage.Dte.StatusBar.Text = "TEMPTEMP: build can continue";

            return VSConstants.S_OK;
        }

        public static LintFilesCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => _package;
        public static void Initialize(Package package) => Instance = new LintFilesCommand(package);
    }
}
