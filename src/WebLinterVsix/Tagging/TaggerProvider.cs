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
            _textDocumentFactoryService = textDocumentFactoryService;
            WebLinterPackage.TaggerProvider = this;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer != textView.TextBuffer || typeof(IErrorTag) != typeof(T) ||
                !_textDocumentFactoryService.TryGetTextDocument(buffer, out ITextDocument document)) return null;
            string extension = Path.GetExtension(document.FilePath)?.ToLowerInvariant();
            if (!IsValidFileExtension(extension)) return null;
            if (!_taggerCache.ContainsKey(textView))
            {
                _taggerCache.Add(textView, new Tagger(buffer, document, textView, this));
                textView.Closed += (s, e) =>
                {
                    _taggerCache[textView].Dispose();
                    _taggerCache.Remove(textView);
                };
            }
            return _taggerCache[textView] as ITagger<T>;
        }

        private bool IsValidFileExtension(string extension)
        {
            // TODO only need JavaScript extensions checked if the option to use them is enabled
            return extension == ".ts" || extension == ".tsx" || extension == ".js" || extension == ".jsx";
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

        //[Conditional("DEBUG")]
        //private void CheckThread()
        //{
        //    if (Thread.CurrentThread.ManagedThreadId != 1) throw new Exception("TaggerProvider not running on UI thread");
        //}

        //[Conditional("DEBUG")]
        //private void DebugDumpTaggers()
        //{
        //    Debug.WriteLine("CURRENT TAGGERS:");
        //    foreach (KeyValuePair<ITextView, Tagger> tagger in _taggerCache)
        //    {
        //        Debug.WriteLine(tagger.Value.FilePath);
        //    }
        //}

    }
}
