// Modifications Copyright Rich Newman 2017
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using WebLinterVsix.Helpers;

namespace WebLinterVsix.FileListeners
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JavaScript")]
    [ContentType("TypeScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class SourceFileCreationListener : IVsTextViewCreationListener
    {
        public SourceFileCreationListener()
        {
            Console.WriteLine("Test");
        }
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            try
            {
                if (WebLinterPackage.Settings == null) return;  // We're not initialized
                IWpfTextView textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
                if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out ITextDocument _document))
                {
                    OnFileOpened(textView, _document);
                }
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        public static void OnFileOpened(IWpfTextView textView, ITextDocument _document)
        {
            if (textView == null || _document == null) return;
            if (textView.Properties.TryGetProperty("lint_filename", out string fileName) && fileName != null) return;
            if (textView.Properties.TryGetProperty("generated", out bool generated) && generated) return;
            if (!LintableFiles.IsValidFile(_document.FilePath)) return;  // Is the filepath valid and does the file exist
            textView.Properties.AddProperty("lint_filename", _document.FilePath);
            textView.Properties.AddProperty("lint_document", _document);
            textView.Closed += TextviewClosed;
            _document.FileActionOccurred += DocumentSaved; // Hook the event whether lintable or not: it may become lintable
            if (!LintableFiles.IsLintableTsTsxJsJsxFile(_document.FilePath)) return;
            // Don't run linter again if error list already contains errors for the file.
            if (!TableDataSource.Instance.HasErrors(_document.FilePath) &&
                    WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested)
            {
                Task.Run(async () =>
                {
                    await CallLinterService(_document.FilePath);
                });
            }

        }

        private static void TextviewClosed(object sender, EventArgs e)
        {
            try
            {
                IWpfTextView view = (IWpfTextView)sender;
                if (view != null) view.Closed -= TextviewClosed;
                if (view != null && view.Properties.TryGetProperty("lint_document", out ITextDocument _document))
                    _document.FileActionOccurred -= DocumentSaved;
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

        private async static void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            try
            {
                if (WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested &&
                    e.FileActionType == FileActionTypes.ContentSavedToDisk &&
                    LintableFiles.IsLintableTsTsxJsJsxFile(e.FilePath)) // We may have changed settings since the event was hooked
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
