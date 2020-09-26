using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
                if (wpfTextView == null || textDocument == null || !IsInSolution(textDocument.FilePath)) return;
                // Legacy: Web Compiler and Bundler & Minifier added this property to generated files
                if (wpfTextView.Properties.TryGetProperty("generated", out bool generated) && generated) return;
                if (!LintableFiles.IsValidFile(textDocument.FilePath)) return;  // Is the filepath valid and does the file exist
                AddTextViewToBufferList(wpfTextView);
                wpfTextView.Properties.AddProperty("lint_document", textDocument);
                wpfTextView.Closed += TextviewClosed;
                // It's possible to open a second textview on the same underlying file/buffer
                if (wpfTextView.TextBuffer.Properties.TryGetProperty("lint_filename", out string fileName) && fileName != null) return;
                wpfTextView.TextBuffer.Properties.AddProperty("lint_filename", textDocument.FilePath);
                //wpfTextView.TextBuffer.PostChanged += TextBuffer_PostChanged;
                //wpfTextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
                textDocument.FileActionOccurred += OnFileActionOccurred; // Hook the event whether lintable or not: it may become lintable
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

        // Both events called on UI thread, both have sender the TextBuffer, which knows the filename
        // So we can do Task.Run(async () => await CallLinterService(filePath) as above
        // We can do it when we're idle only
        // https://www.syncfusion.com/faq/wpf/threading/what-is-the-equivalent-of-the-windows-forms-onidle-event-in-wpf
        // We can do it after a sleep with a cancellation token so if this is called again we cancel the lint
        // It's not so easy to stop a running lint from updating though
        private static void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e)
        {
            Debug.WriteLine($"In TextBuffer_ChangedLowPriority, thread={System.Threading.Thread.CurrentThread.ManagedThreadId}");
        }
        private static void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            Debug.WriteLine($"In TextBuffer_PostChanged, thread={System.Threading.Thread.CurrentThread.ManagedThreadId}");
            ITextBuffer textBuffer = sender as ITextBuffer;
            if (textBuffer != null && textBuffer.Properties.TryGetProperty("lint_filename", out string fileName))
            {
                Task.Run(async () =>
                {
                    await CallLinterService(fileName);
                });
            }
        }

        // This is clunky.  Is there a better way of tracking text views for a buffer?
        private static void AddTextViewToBufferList(IWpfTextView wpfTextView)
        {
            if (wpfTextView.TextBuffer.Properties.TryGetProperty("textview_list", out List<IWpfTextView> textViewList)
                && textViewList != null)
                textViewList.Add(wpfTextView);
            else
                wpfTextView.TextBuffer.Properties.AddProperty("textview_list", new List<IWpfTextView> { wpfTextView });
        }

        // Returns true if list is empty after the removal
        private static bool RemoveTextViewFromBufferList(IWpfTextView wpfTextView)
        {
            List<IWpfTextView> textViewList = (List<IWpfTextView>)wpfTextView.TextBuffer.Properties["textview_list"];
            textViewList.Remove(wpfTextView);
            return textViewList.Count == 0;
        }

        private static bool IsInSolution(string fileName)
        {
            if (WebLinterPackage.Dte == null) return false;
            ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(fileName);
            return item?.GetFullPath() is string;
        }

        private static void TextviewClosed(object sender, EventArgs e)
        {
            try
            {
                IWpfTextView wpfTextView = (IWpfTextView)sender;
                wpfTextView.Closed -= TextviewClosed;
                bool bufferClosing = RemoveTextViewFromBufferList(wpfTextView);
                if (!bufferClosing) return;
                if (wpfTextView.Properties.TryGetProperty("lint_document", out ITextDocument textDocument))
                    textDocument.FileActionOccurred -= OnFileActionOccurred;
                if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.OnlyRunIfRequested) return;

                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    if (wpfTextView.TextBuffer.Properties.TryGetProperty("lint_filename", out string fileName))
                    {
                        // TODO there's no locking on TableDataSource, which I think has to have thread affinity to the UI thread
                        // So we can't call it from a threadpool thread
                        TableDataSource.Instance.CleanErrors(new[] { fileName });
                    }
                });
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        private async static void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            try
            {
                if (WebLinterPackage.Settings != null && !WebLinterPackage.Settings.OnlyRunIfRequested &&
                    (e.FileActionType == FileActionTypes.ContentSavedToDisk || e.FileActionType == FileActionTypes.DocumentRenamed) &&
                    LintableFiles.IsLintableTsTsxJsJsxFile(e.FilePath)) // We may have changed settings since the event was hooked
                {
                    await CallLinterService(e.FilePath);
                }
                if (e.FileActionType == FileActionTypes.DocumentRenamed)
                {
                    ITextBuffer textBuffer = (sender as ITextDocument)?.TextBuffer;
                    if (textBuffer != null && textBuffer.Properties.TryGetProperty("lint_filename", out string oldFileName))
                    {
                        TableDataSource.Instance.CleanErrors(new[] { oldFileName });
                        textBuffer.Properties["lint_filename"] = e.FilePath;
                    }
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
