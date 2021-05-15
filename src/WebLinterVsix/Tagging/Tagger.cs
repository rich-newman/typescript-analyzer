using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using WebLinter;

namespace WebLinterVsix.Tagging
{
    class Tagger : ITagger<LintingErrorTag> //, IDisposable
    {
        private readonly ITextDocument _document;
        private ITextSnapshot _currentTextSnapshot;

        // FilePath can change whilst the tagger is in use if we rename an open file, so don't key on it
        // _document, _buffer, and _textView are all always the same object for a given tagger because we create a new tagger
        // if the view changes.
        internal string FilePath => _document.FilePath;

        internal Tagger(ITextBuffer buffer, ITextDocument document)
        {
            CheckThread();
            Debug.WriteLine($"Creating Tagger for {document.FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
            _document = document;
            _currentTextSnapshot = buffer.CurrentSnapshot;
            //this.TagsChanged += OnTagsChanged;
        }

        //private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
        //{
        //    Debug.WriteLine($"OnTagsChanged: text {e.Span.GetText()}, file={FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
        //}

        [Conditional("DEBUG")]
        private void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1) throw new Exception("Tagger not running on UI thread");
        }

        private bool _isFixing = false;
        public void RefreshTags(bool isFixing)
        {
            CheckThread();
            _isFixing = isFixing;  // If we fix we need to calculate tags on the next snapshot
            _tagSpans = null;
            Debug.WriteLine($"In RefreshTags calling TagsChanged, file={FilePath}, "
                + $"thread={Thread.CurrentThread.ManagedThreadId}");
            TagsChanged?.Invoke(this,
                new SnapshotSpanEventArgs(new SnapshotSpan(_currentTextSnapshot, 0, _currentTextSnapshot.Length)));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

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
#if DEBUG
                CheckThread();
                Debug.WriteLine($"GetTags: File={FilePath}, New TextSnapshot version={spans[0].Snapshot.Version}, " +
                    $"old TextSnapshot version={_currentTextSnapshot.Version}, thread={Thread.CurrentThread.ManagedThreadId}");
                if (!IsSpansValid(spans)) throw new Exception("Invalid spans in GetTags");
                if (IsTextBufferChanged(spans)) throw new Exception("Text buffer changed in Tagger");
#endif
                bool isTextSnapshotChanged = IsTextSnapshotChanged(spans);
                if (isTextSnapshotChanged) _currentTextSnapshot = spans[0].Snapshot;
                if (_isFixing && isTextSnapshotChanged) // Force recalc when fixing and the text snapshot changes
                {
                    _isFixing = false;
                    CalculateTagSpans();
                }
                else if (_tagSpans == null)
                    CalculateTagSpans();
                else if (isTextSnapshotChanged)
                    UpdateTagSpansForNewTextSnapshot();
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

#if DEBUG
        private bool IsSpansValid(NormalizedSnapshotSpanCollection spans) =>
            (spans?.Count ?? 0) > 0 && spans[0].Snapshot?.TextBuffer != null;

        private bool IsTextBufferChanged(NormalizedSnapshotSpanCollection spans) =>
            spans[0].Snapshot.TextBuffer != _currentTextSnapshot.TextBuffer;
#endif

        private bool IsTextSnapshotChanged(NormalizedSnapshotSpanCollection spans) =>
            spans[0].Snapshot != _currentTextSnapshot;

        private List<ITagSpan<LintingErrorTag>> _tagSpans = null;
        private void CalculateTagSpans()
        {
            Debug.WriteLine($"CalculateTagSpans, file={FilePath}, thread={Thread.CurrentThread.ManagedThreadId}");
            _tagSpans = new List<ITagSpan<LintingErrorTag>>();
            if (!ErrorListDataSource.Snapshots.ContainsKey(FilePath)) return;
            List<LintingError> errors = ErrorListDataSource.Snapshots[FilePath].Errors;  // Immutable snapshot
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
            Debug.WriteLine($"UpdateTagSpansForNewTextSnapshot, file={FilePath}: " +
                $"New TextSnapshot version={_currentTextSnapshot.Version}, thread={Thread.CurrentThread.ManagedThreadId}");
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
