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
                // If we open a folder then our package isn't initialized but this class does get created
                // We get failures in the events because Settings is null, so don't do anything if that's the case
                if (WebLinterPackage.Settings == null) return;
                IWpfTextView textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
                textView.Closed += TextviewClosed;

                // Both "Web Compiler" and "Bundler & Minifier" extensions add this property on their
                // generated output files. Generated output should be ignored from linting
                if (textView.Properties.TryGetProperty("generated", out bool generated) && generated) return;

                if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out ITextDocument _document))
                {
                    if (!LintableFiles.IsValidFile(_document.FilePath)) return;
                    _document.FileActionOccurred += DocumentSaved;
                    textView.Properties.AddProperty("lint_filename", _document.FilePath);
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
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
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
