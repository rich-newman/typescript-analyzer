// Modifications Copyright Rich Newman 2017
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace WebLinterVsix
{
    internal sealed class LintFilesCommand : LintFilesCommandBase
    {
        private readonly Package _package;

        private LintFilesCommand(Package package)
        {
            _package = package ?? throw new ArgumentNullException("package");
            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(PackageGuids.WebLinterCmdSet, PackageIds.LintFilesCommand);
                var menuItem = new OleMenuCommand(async (s, e) => { await LintSelectedFiles(false); }, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static LintFilesCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => _package;
        public static void Initialize(Package package) => Instance = new LintFilesCommand(package);
    }
}
