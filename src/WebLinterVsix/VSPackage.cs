using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using WebLinter;

namespace WebLinterVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", WebLinter.Constants.VERSION, IconResourceID = 400)]
    [ProvideOptionPage(typeof(Settings), "TypeScript Analyzer", "TSLint", 101, 111, true, new[] { "tslint" }, ProvidesLocalizedCategoryName = false)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.guidVSPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WebLinterPackage : AsyncPackage
    {
        public static DTE2 Dte;
        public static ISettings Settings;
        private SolutionEvents _events;

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Dte = GetService(typeof(DTE)) as DTE2;
            Settings = (Settings)GetDialogPage(typeof(Settings));

            _events = Dte.Events.SolutionEvents;
            _events.AfterClosing += delegate { TableDataSource.Instance.CleanAllErrors(); };

            Logger.Initialize(this, Vsix.Name);
            LintFilesCommand.Initialize(this);
            FixLintErrorsCommand.Initialize(this);
            CleanErrorsCommand.Initialize(this);
            EditConfigFilesCommand.Initialize(this);
            ResetConfigFilesCommand.Initialize(this);

            base.Initialize();
        }

        public IVsOutputWindow GetIVsOutputWindow() => (IVsOutputWindow)GetService(typeof(SVsOutputWindow));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Linter.Server.Down();
            }

            base.Dispose(true);
        }
    }
}