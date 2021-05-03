// Modifications Copyright Rich Newman 2017
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using WebLinter;
using System.Linq;

namespace WebLinterVsix
{
    internal sealed class CleanErrorsCommand
    {
        private readonly Package _package;
        private readonly BuildEvents _events;

        private CleanErrorsCommand(Package package)
        {
            _package = package;
            _events = WebLinterPackage.Dte.Events.BuildEvents;
            _events.OnBuildBegin += OnBuildBegin;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var menuCommandID = new CommandID(PackageGuids.WebLinterCmdSet, PackageIds.CleanErrorsCommand);
            var menuItem = new OleMenuCommand(CleanErrors, menuCommandID);
            menuItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        private void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            // Called on UI thread
            if (WebLinterPackage.Settings.CleanErrorsOnBuild &&
               (Action == vsBuildAction.vsBuildActionClean ||
               (Action == vsBuildAction.vsBuildActionRebuildAll && !WebLinterPackage.Settings.RunOnBuild)))
            {
                ErrorListDataSource.Instance.CleanAllErrors();
            }
        }

        public static CleanErrorsCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new CleanErrorsCommand(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            // Called on UI thread
            var button = (OleMenuCommand)sender;

            button.Visible = ErrorListDataSource.Instance.HasErrors();
        }

        private void CleanErrors(object sender, EventArgs e)
        {
            // Called on UI thread
            ErrorListDataSource.Instance.CleanAllErrors();
            ErrorListDataSource.Instance.RaiseErrorListChanged();
        }
    }
}
