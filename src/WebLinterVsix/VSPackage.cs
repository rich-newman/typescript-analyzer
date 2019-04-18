using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
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

            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if (isSolutionLoaded)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                HandleOpenSolution();
            }

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
            foreach (object windowObject in Dte.Windows)
            {
                if (windowObject is Window window && window.Type == vsWindowType.vsWindowTypeDocument && window.Document != null)
                {
                    IVsTextView vsTextView = GetIVsTextView(window.Document.FullName);
                    if (vsTextView == null) continue;
                    IWpfTextView wpfTextView = GetWpfView(vsTextView);
                    if (wpfTextView == null) continue;
                    ITextDocument _document = GetTextDocument(wpfTextView);
                    if (_document == null) continue;
                    WebLinterVsix.FileListeners.SourceFileCreationListener.OnFileOpened(wpfTextView, _document);
                }
            }
        }

        private ITextDocument GetTextDocument(IWpfTextView wpfTextView)
        {
            IComponentModel componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            ITextDocumentFactoryService textDocumentFactoryService = componentModel.GetService<ITextDocumentFactoryService>();
            if (textDocumentFactoryService.TryGetTextDocument(wpfTextView.TextDataModel.DocumentBuffer, out ITextDocument _document))
                return _document;
            else
                return null;
        }

        // https://stackoverflow.com/questions/45751908/how-to-get-iwpftextview-from-command-visual-studio-extension-2017
        private IWpfTextView GetWpfView(IVsTextView textViewCurrent)
        {
            IComponentModel componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            IVsEditorAdaptersFactoryService editor = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            return editor.GetWpfTextView(textViewCurrent);
        }

        // https://stackoverflow.com/questions/2413530/find-an-ivstextview-or-iwpftextview-for-a-given-projectitem-in-vs-2010-rc-exten/2427368#2427368
        internal IVsTextView GetIVsTextView(string filePath)
        {
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)GetGlobalService(typeof(SDTE));
            ServiceProvider serviceProvider = new ServiceProvider(sp);
            if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty, out _, out _, out IVsWindowFrame windowFrame))
                return VsShellUtilities.GetTextView(windowFrame);
            else
                return null;
        }

        public IVsOutputWindow GetIVsOutputWindow() => (IVsOutputWindow)GetService(typeof(SVsOutputWindow));

        // public static ISettings Settings;
        public ISettings GetSettings() {
            var result = (Settings)GetDialogPage(typeof(Settings));
            return result;
        }

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