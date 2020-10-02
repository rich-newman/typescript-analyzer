using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using WebLinter;

namespace WebLinterVsix.Tagging
{
    // TODO as a first cut we don't re-run the linter for any new reasons
    // We have to consider what happens if we edit a file to the underlining - we can end up in the wrong place
    // before a save
    class Tagger : ITagger<LintingErrorTag>, IDisposable
    {
        private bool _disposed = false; // TODO remove disposed flag after testing
        public void Dispose() 
        {
            TableDataSource.Instance.ErrorListChanged -= OnErrorListChanged;
            _disposed = true;
        }
        // TODO remove buffer if we don't have a need for it
        private ITextBuffer _buffer;
        private ITextDocument _document;
        private ITextView _textView;
        private TaggerProvider _taggerProvider;
        private ITextSnapshot _currentTextSnapshotUnderlying;
        private ITextSnapshot _currentTextSnapshot // TODO set back _currentTextSnapshot to field after testing
        {
            get 
            {
                CheckThread();
                return _currentTextSnapshotUnderlying; 
            }
            set 
            {
                CheckThread();
                _currentTextSnapshotUnderlying = value; 
            }
        }

        // TODO The GetCurrentTextSnapshotVersion method is used for testing only
        internal string GetCurrentTextSnapshotVersion() => _currentTextSnapshot.Version.ToString();

        // FilePath can change whilst the tagger is in use if we rename an open file, so don't key on it
        // _document, _buffer, and _textView are all always the same object for a given tagger because we create a new tagger
        // if the view changes.
        internal string FilePath
        {
            get 
            {
                // TODO testing code only - just return _document.FilePath
                ITextDocument currentDocument = _taggerProvider.GetDocument(_buffer);
                if (currentDocument != _document) throw new Exception("Document has changed for tagger");
                return _document.FilePath;
            }
        }

        internal Tagger(ITextBuffer buffer, ITextDocument document, ITextView textView, TaggerProvider taggerProvider)
        {
            CheckThread();
            Debug.WriteLine($"Creating Tagger for {document.FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
            _buffer = buffer;
            _document = document;
            _textView = textView;
            _taggerProvider = taggerProvider;  // TODO remove reference to provider: it's only for testing, the tagger doesn't need to know
            _currentTextSnapshot = buffer.CurrentSnapshot;
            //_document.FileActionOccurred += OnFileActionOccurred;
            //_buffer.ChangedLowPriority += OnBufferChanged;
            this.TagsChanged += OnTagsChanged;
            TableDataSource.Instance.ErrorListChanged += OnErrorListChanged;
        }

        private void OnErrorListChanged(object sender, EventArgs e)
        {
            try
            {
                CheckThread();
                _tagSpans = null;
                Debug.WriteLine($"In OnErrorListChanged calling TagsChanged, file={FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
                // TODO we can get CalculateTagSpans to calculate start and end of tag ranges
                RaiseTagsChanged();
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }

        }

        private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            Debug.WriteLine($"OnTagsChanged: text {e.Span.GetText()}, file={FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
        }

        [Conditional("DEBUG")]
        private void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1) throw new Exception("Tagger not running on UI thread");
            if (_disposed) throw new Exception("Called method on disposed Tagger");
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        public void RaiseTagsChanged()
        {
            CheckThread();
            TagsChanged?.Invoke(this,
                new SnapshotSpanEventArgs(new SnapshotSpan(_currentTextSnapshot, 0, _currentTextSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<LintingErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (WebLinterPackage.Settings == null || !WebLinterPackage.Settings.ShowUnderlining) yield break;
            UpdateTagSpans(spans);
            foreach (ITagSpan<LintingErrorTag> tagSpan in _tagSpans)
            {
                if (spans.IntersectsWith(tagSpan.Span))
                    yield return tagSpan;
            }
        }

        private void UpdateTagSpans(NormalizedSnapshotSpanCollection spans)
        {
            try
            {
                CheckThread();
                Debug.WriteLine($"GetTags: File={FilePath}, New TextSnapshot version={spans[0].Snapshot.Version}, old TextSnapshot version={_currentTextSnapshot.Version}, thread={Thread.CurrentThread.ManagedThreadId}");
                if (!IsSpansValid(spans)) throw new Exception("Invalid spans in GetTags");  // TODO can take out if it never throws
                bool isTextBufferChanged = IsTextBufferChanged(spans);
                if (isTextBufferChanged) throw new Exception("Text buffer changed in Tagger: should have stopped this");
                bool isTextSnapshotChanged = IsTextSnapshotChanged(spans);
                if (isTextSnapshotChanged) _currentTextSnapshot = spans[0].Snapshot;
                // TODO this works if the buffer is changed, but makes no sense here since we're throwing above in that case
                if (isTextBufferChanged) _buffer = spans[0].Snapshot.TextBuffer;
                if (_tagSpans == null || isTextBufferChanged)
                    CalculateTagSpans();
                else if (isTextSnapshotChanged)
                    UpdateTagSpansForNewTextSnapshot();
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        private bool IsSpansValid(NormalizedSnapshotSpanCollection spans) => 
            (spans?.Count ?? 0) > 0 && spans[0].Snapshot?.TextBuffer != null;

        private bool IsTextBufferChanged(NormalizedSnapshotSpanCollection spans) => 
            spans[0].Snapshot.TextBuffer != _currentTextSnapshot.TextBuffer;

        private bool IsTextSnapshotChanged(NormalizedSnapshotSpanCollection spans) =>
            spans[0].Snapshot != _currentTextSnapshot;

        private List<ITagSpan<LintingErrorTag>> _tagSpans = null;
        private void CalculateTagSpans()
        {
            Debug.WriteLine($"CalculateTagSpans, file={FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
            _tagSpans = new List<ITagSpan<LintingErrorTag>>();
            if (!TableDataSource.Snapshots.ContainsKey(FilePath)) return;
            List<LintingError> errors = TableDataSource.Snapshots[FilePath].Errors;  // Immutable snapshot
            if (errors == null || errors.Count == 0) return;
            foreach (LintingError lintingError in errors)
            {
                LintingErrorTag lintingErrorTag = new LintingErrorTag(lintingError);
                SnapshotPoint startSnapshotPoint = CalculateSnapshotPoint(lintingError.LineNumber, lintingError.ColumnNumber);
                SnapshotPoint endSnapshotPoint = IsEndProvided(lintingError) ? 
                    CalculateSnapshotPoint(lintingError.EndLineNumber.Value, lintingError.EndColumnNumber.Value) : 
                    startSnapshotPoint;  // snapshot [1, 1) does include character at 1
                SnapshotSpan snapshotSpan = new SnapshotSpan(startSnapshotPoint, endSnapshotPoint);
                ITagSpan<LintingErrorTag> tagSpan = new TagSpan<LintingErrorTag>(snapshotSpan, lintingErrorTag);
                _tagSpans.Add(tagSpan);
            }
        }

        private SnapshotPoint CalculateSnapshotPoint(int lineNumber, int columnNumber) =>
            _currentTextSnapshot.GetLineFromLineNumber(lineNumber).Start.Add(columnNumber);

        private bool IsEndProvided(LintingError lintingError) => 
            lintingError.EndColumnNumber != null && lintingError.EndLineNumber != null;

        public void UpdateTagSpansForNewTextSnapshot()
        {
            Debug.WriteLine($"UpdateTagSpansForNewTextSnapshot, file={FilePath}: New TextSnapshot version={_currentTextSnapshot.Version}, thread={Thread.CurrentThread.ManagedThreadId}");
            List<ITagSpan<LintingErrorTag>> newTagSpans = new List<ITagSpan<LintingErrorTag>>();
            foreach (ITagSpan<LintingErrorTag> tagSpan in _tagSpans)
            {
                SnapshotSpan newSpan = tagSpan.Span.TranslateTo(_currentTextSnapshot, SpanTrackingMode.EdgeExclusive);
                if (newSpan != null) newTagSpans.Add(new TagSpan<LintingErrorTag>(newSpan, tagSpan.Tag));
            }
            _tagSpans = newTagSpans;
        }
    }
}
