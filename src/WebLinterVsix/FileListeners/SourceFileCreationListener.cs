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

        private ITextDocument _document;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            // If we open a folder then our package isn't initialized but this class does get created
            // We get failures in the events because Settings is null, so don't do anything if that's the case
            if (WebLinterPackage.Settings == null) return;
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            textView.Closed += TextviewClosed;

            // Both "Web Compiler" and "Bundler & Minifier" extensions add this property on their
            // generated output files. Generated output should be ignored from linting
            bool generated;
            if (textView.Properties.TryGetProperty("generated", out generated) && generated)
                return;

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                Task.Run(async () =>
                {
                    _document.FileActionOccurred += DocumentSaved;

                    if (!LinterService.IsLintableTsOrTsxFile(_document.FilePath))
                        return;

                    textView.Properties.AddProperty("lint_filename", _document.FilePath);

                    // Don't run linter again if error list already contains errors for the file.
                    if (!TableDataSource.Instance.HasErrors(_document.FilePath) &&
                            WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested)
                    {
                        await CallLinterService(_document.FilePath);
                    }
                });
            }
        }

        private void TextviewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;
            if (view != null) view.Closed -= TextviewClosed;
            if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.OnlyRunIfRequested) return;

            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                string fileName;

                if (view != null && view.Properties.TryGetProperty("lint_filename", out fileName))
                {
                    TableDataSource.Instance.CleanErrors(new[] { fileName });
                }
            });

        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested &&
                e.FileActionType == FileActionTypes.ContentSavedToDisk &&
                LinterService.IsLintableTsOrTsxFile(e.FilePath)) // We may have changed settings since the event was hooked
            {
                await CallLinterService(e.FilePath);
            }
        }

        private static async Task CallLinterService(string filePath)
        {
            if (WebLinterPackage.Settings.UseTsConfig)
            {
                Tsconfig tsconfig = TsconfigLocations.FindFromProjectItem(filePath, WebLinterPackage.Dte.Solution);
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
