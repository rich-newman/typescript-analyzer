using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WebLinterVsix.Tagging
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [ContentType("projection")]
    [TagType(typeof(IErrorTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    public class TaggerProvider : IViewTaggerProvider
    {
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;

        [ImportingConstructor]
        public TaggerProvider([Import] ITextDocumentFactoryService textDocumentFactoryService)
        {
            CheckThread();
            _textDocumentFactoryService = textDocumentFactoryService;
            WebLinterPackage.TaggerProvider = this;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            CheckThread();
            if (buffer != textView.TextBuffer || typeof(IErrorTag) != typeof(T) ||
                !_textDocumentFactoryService.TryGetTextDocument(buffer, out ITextDocument document)) return null;
            string extension = Path.GetExtension(document.FilePath)?.ToLowerInvariant();
            if (extension != ".ts") return null; // TODO other extensions
            if (!_taggerCache.ContainsKey(textView))
            {
                _taggerCache.Add(textView, new Tagger(buffer, document, textView, this));
                document.FileActionOccurred += Document_FileActionOccurred;
                textView.Closed += (s, e) =>
                {
                    CheckThread();
                    _taggerCache[textView].Dispose();
                    _taggerCache.Remove(textView);
                };
            }
            return _taggerCache[textView] as ITagger<T>;
        }

        private void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if(e.FileActionType== FileActionTypes.DocumentRenamed)
            {
                string test = e.FilePath;  // New filepath, and that's all she wrote, so absolutely useless
            }
        }

        // TODO remove GetDocument method
        internal ITextDocument GetDocument(ITextBuffer buffer)
        {
            if (!_textDocumentFactoryService.TryGetTextDocument(buffer, out ITextDocument newDocument)) return null;
            return newDocument;
        }

        // I'm pretty sure we're always on the UI thread here: let's blow up in debug if that's ever not true
        // TODO look at the calling code rather than random exceptions to check threading in the tagger code
        [Conditional("DEBUG")]
        private void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1) throw new Exception("TaggerProvider not running on UI thread");
        }

        // We key on ITextView (rather than filenames) because of renames with open files, when the text view remains
        // the same but the file name changes (and blows up the code)
        private Dictionary<ITextView, Tagger> _taggerCache = new Dictionary<ITextView, Tagger>();

        public void Settings_ShowUnderliningChanged(object sender, EventArgs e)
        {
            foreach (KeyValuePair<ITextView, Tagger> tagger in _taggerCache)
            {
                tagger.Value.RaiseTagsChanged();
            }
        }

        [Conditional("DEBUG")]
        private void DebugDumpTaggers()
        {
            Debug.WriteLine("CURRENT TAGGERS:");
            foreach (KeyValuePair<ITextView, Tagger> tagger in _taggerCache)
            {
                Debug.WriteLine(tagger.Value.FilePath);
            }
        }

    }
}
