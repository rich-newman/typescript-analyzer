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
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            try
            {
                IWpfTextView wpfTextView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
                if (TextDocumentFactoryService.TryGetTextDocument(wpfTextView.TextDataModel.DocumentBuffer, out ITextDocument textDocument))
                {
                    if (WebLinterPackage.Settings == null)
                        WebLinterPackage.UnhandledStartUpFiles.Add(new Tuple<IWpfTextView, ITextDocument>(wpfTextView, textDocument));
                    else
                        OnFileOpened(wpfTextView, textDocument);
                }
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        public static void OnFileOpened(IWpfTextView wpfTextView, ITextDocument textDocument)
        {
            try
            {

                if (wpfTextView == null || textDocument == null) return;
                if (wpfTextView.Properties.TryGetProperty("lint_filename", out string fileName) && fileName != null) return;
                if (wpfTextView.Properties.TryGetProperty("generated", out bool generated) && generated) return;
                if (!LintableFiles.IsValidFile(textDocument.FilePath)) return;  // Is the filepath valid and does the file exist
                wpfTextView.Properties.AddProperty("lint_filename", textDocument.FilePath);
                wpfTextView.Properties.AddProperty("lint_document", textDocument);
                wpfTextView.Closed += TextviewClosed;
                textDocument.FileActionOccurred += DocumentSaved; // Hook the event whether lintable or not: it may become lintable
                if (!LintableFiles.IsLintableTsTsxJsJsxFile(textDocument.FilePath)) return;
                // Don't run linter again if error list already contains errors for the file.
                if (!TableDataSource.Instance.HasErrors(textDocument.FilePath) &&
                        WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested)
                {
                    Task.Run(async () =>
                    {
                        await CallLinterService(textDocument.FilePath);
                    });
                }
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        private static void TextviewClosed(object sender, EventArgs e)
        {
            try
            {
                IWpfTextView wpfTextView = (IWpfTextView)sender;
                if (wpfTextView != null) wpfTextView.Closed -= TextviewClosed;
                if (wpfTextView != null && wpfTextView.Properties.TryGetProperty("lint_document", out ITextDocument textDocument))
                    textDocument.FileActionOccurred -= DocumentSaved;
                if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.OnlyRunIfRequested) return;

                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    if (wpfTextView != null && wpfTextView.Properties.TryGetProperty("lint_filename", out string fileName))
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
