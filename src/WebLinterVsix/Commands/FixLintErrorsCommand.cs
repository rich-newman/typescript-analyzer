using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using WebLinter;

namespace WebLinterVsix
{
    internal sealed class FixLintErrorsCommand : LintFilesCommandBase
    {
        private readonly Package _package;

        private FixLintErrorsCommand(Package package)
        {
            _package = package ?? throw new ArgumentNullException("package");
            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(PackageGuids.WebLinterCmdSet, PackageIds.FixLintErrorsCommand);
                var menuItem = new OleMenuCommand(async (s, e) => { await LintSelectedFiles(true); }, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static FixLintErrorsCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => _package;
        public static void Initialize(Package package) => Instance = new FixLintErrorsCommand(package);
    }
}
