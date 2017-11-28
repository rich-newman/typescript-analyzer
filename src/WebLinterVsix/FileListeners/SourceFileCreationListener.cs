// Modifications Copyright Rich Newman 2017
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using WebLinterVsix.Helpers;

namespace WebLinterVsix.FileListeners
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("TypeScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class SourceFileCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            Logger.Log("VsTextViewCreated entered, thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId);
            try
            {
                // If we open a folder then our package isn't initialized but this class does get created
                // We get failures in the events because Settings is null, so don't do anything if that's the case
                if (WebLinterPackage.Settings == null) return;
                var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
                textView.Closed += TextviewClosed;

                // Both "Web Compiler" and "Bundler & Minifier" extensions add this property on their
                // generated output files. Generated output should be ignored from linting
                if (textView.Properties.TryGetProperty("generated", out bool generated) && generated) return;

                if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out ITextDocument _document))
                {
                    Logger.Log("VsTextViewCreated got document, thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId + ", document=" + _document.FilePath);
                    if (!LintableFiles.IsLintableTsOrTsxFile(_document.FilePath)) return;
                    _document.FileActionOccurred += DocumentSaved;
                    textView.Properties.AddProperty("lint_filename", _document.FilePath);
                    // Don't run linter again if error list already contains errors for the file.
                    if (!TableDataSource.Instance.HasErrors(_document.FilePath) &&
                            WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested)
                    {
                        Task.Run(async () =>
                        {
                            Logger.Log("VsTextViewCreated calling linter service, thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId + ", document=" + _document.FilePath);
                            await CallLinterService(_document.FilePath);
                            Logger.Log("VsTextViewCreated returned from linter service, thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId + ", document=" + _document.FilePath);
                        });
                    }
                }
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
            Logger.Log("VsTextViewCreated exited, thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        private void TextviewClosed(object sender, EventArgs e)
        {
            try
            {
                IWpfTextView view = (IWpfTextView)sender;
                if (view != null) view.Closed -= TextviewClosed;
                if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.OnlyRunIfRequested) return;

                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    if (view != null && view.Properties.TryGetProperty("lint_filename", out string fileName))
                    {
                        TableDataSource.Instance.CleanErrors(new[] { fileName });
                    }
                });
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            try
            {
                if (WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested &&
                    e.FileActionType == FileActionTypes.ContentSavedToDisk &&
                    LintableFiles.IsLintableTsOrTsxFile(e.FilePath)) // We may have changed settings since the event was hooked
                {
                    await CallLinterService(e.FilePath);
                }
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        private static async Task CallLinterService(string filePath)
        {
            if (WebLinterPackage.Settings.UseTsConfig)
            {
                Tsconfig tsconfig = TsconfigLocations.FindFromProjectItem(filePath);
                if (tsconfig == null) return;
                await LinterService.Lint(false, false, false, new string[] { tsconfig.FullName },
                                                              new string[] { filePath });
            }
            else
            {
                await LinterService.Lint(false, false, false, new string[] { filePath });
            }
        }
    }
}
