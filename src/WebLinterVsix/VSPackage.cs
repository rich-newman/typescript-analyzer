using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
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
        public static List<Tuple<IWpfTextView, ITextDocument>> UnhandledStartUpFiles = new List<Tuple<IWpfTextView, ITextDocument>>();

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Dte = GetService(typeof(DTE)) as DTE2;
            Settings = (Settings)GetDialogPage(typeof(Settings));

            _events = Dte.Events.SolutionEvents;
            _events.AfterClosing += delegate { TableDataSource.Instance.CleanAllErrors(); };

            Logger.Initialize(this, Vsix.Name);

            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if (isSolutionLoaded) HandleOpenSolution();

            LintFilesCommand.Initialize(this);
            FixLintErrorsCommand.Initialize(this);
            CleanErrorsCommand.Initialize(this);
            EditConfigFilesCommand.Initialize(this);
            ResetConfigFilesCommand.Initialize(this);

            base.Initialize();
        }

        private async System.Threading.Tasks.Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsSolution solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
            return value is bool isSolOpen && isSolOpen;
        }

        private void HandleOpenSolution(object sender = null, EventArgs e = null)
        {
            foreach (Tuple<IWpfTextView, ITextDocument> tuple in UnhandledStartUpFiles)
            {
                WebLinterVsix.FileListeners.SourceFileCreationListener.OnFileOpened(tuple.Item1, tuple.Item2);
            }
            UnhandledStartUpFiles.Clear();
        }

        public IVsOutputWindow GetIVsOutputWindow() => (IVsOutputWindow)GetService(typeof(SVsOutputWindow));

        protected override void Dispose(bool disposing)
        {
            if (disposing) Linter.Server.Down();
            base.Dispose(true);
        }
    }
}