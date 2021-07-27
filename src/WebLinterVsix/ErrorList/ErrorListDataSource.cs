using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using WebLinter;
using EnvDTE;
using System.Diagnostics;

namespace WebLinterVsix
{
    internal interface IErrorListDataSource
    {
        void AddErrors(IEnumerable<LintingError> errors, Dictionary<string, string> fileToProjectMap);
        void CleanErrors(IEnumerable<string> files);
        void BringToFront();
        void CleanAllErrors();
        void CleanJsJsxErrors();
        bool HasErrors();
        bool HasErrors(string fileName);
        bool HasJsJsxErrors();
    }

    internal class ErrorListDataSource : ITableDataSource, IErrorListDataSource
    {
        private static IErrorListDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();

        //internal static Dictionary<string, TableEntriesSnapshot> Snapshots { get; }
        //      = new Dictionary<string, TableEntriesSnapshot>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, TableEntriesSnapshot> _snapshots = 
            new Dictionary<string, TableEntriesSnapshot>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, TableEntriesSnapshot> Snapshots
        {
            get
            {
                CheckThread();
                return _snapshots;
            }
        }

        [Import]
        private ITableManagerProvider TableManagerProvider { get; set; } = null;

        private ErrorListDataSource()
        {
            CheckThread();
            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

            ITableManager manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            manager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                    StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                    StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                    StandardTableColumnDefinitions.ErrorCategory,
                                    StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                    StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column);
        }

        internal static void InjectMockErrorListDataSource(IErrorListDataSource instance) => _instance = instance;

        public static IErrorListDataSource Instance
        {
            get
            {
                CheckThread();
                if (_instance == null)
                    _instance = new ErrorListDataSource();

                return _instance;
            }
        }

        [Conditional("DEBUG")]
        private static void CheckThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1 
                && _instance?.GetType() == typeof(ErrorListDataSource))
                throw new Exception("ErrorListDataSource not running on UI thread");
        }

        #region ITableDataSource members
        public string SourceTypeIdentifier
        {
            get { return StandardTableDataSources.ErrorTableDataSource; }
        }

        public string Identifier
        {
            get { return PackageGuids.guidVSPackageString; }
        }

        public string DisplayName
        {
            get { return Vsix.Name; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            //CheckThread();  // Not necessarily called on UI thread
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            _managers.Add(manager);
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            CheckThread();
            _managers.Remove(manager);
        }

        public void UpdateAllSinks()
        {
            CheckThread();
            foreach (var manager in _managers)
                manager.UpdateSink(Snapshots.Values);
        }

        public void AddErrors(IEnumerable<LintingError> errors, Dictionary<string, string> fileToProjectMap)
        {
            CheckThread();
            if (errors == null || !errors.Any()) return;
            var cleanErrors = errors.Where(e => e != null && !string.IsNullOrEmpty(e.FileName));
            //DebugDumpMap(fileNameToProjectNameMap);
            foreach (IGrouping<string, LintingError> error in cleanErrors.GroupBy(t => t.FileName))
            {
                fileToProjectMap.TryGetValue(error.Key, out string projectName);
                TableEntriesSnapshot snapshot = new TableEntriesSnapshot(error.Key, projectName, error);
                Snapshots[error.Key] = snapshot;
            }
            UpdateAllSinks();
        }

        //[Conditional("DEBUG")]
        //private void DebugDumpMap(Dictionary<string, string> map)
        //{
        //    foreach (var item in map)
        //        Debug.WriteLine(item.Key + ":" + item.Value);
        //}

        public void CleanJsJsxErrors()
        {
            CheckThread();
            List<string> fileNames = new List<string>();
            foreach (string fileName in Snapshots.Keys)
            {
                if (IsJsOrJsxFile(fileName)) 
                    fileNames.Add(fileName);
            }
            CleanErrors(fileNames);
        }

        public void CleanErrors(IEnumerable<string> files)
        {
            CheckThread();
            foreach (string file in files)
            {
                if (Snapshots.ContainsKey(file))
                {
                    Snapshots[file].Dispose();
                    Snapshots.Remove(file);
                }
            }

            foreach (var manager in _managers)
                manager.RemoveSnapshots(files);

            UpdateAllSinks();
        }

        public void CleanAllErrors()
        {
            CheckThread();
            foreach (string file in Snapshots.Keys)
            {
                var snapshot = Snapshots[file];
                if (snapshot != null) snapshot.Dispose();
            }
            Snapshots.Clear();
            foreach (var manager in _managers)
                manager.Clear();

            UpdateAllSinks();
        }

        public void BringToFront()
        {
            CheckThread();
            WebLinterPackage.Dte.ExecuteCommand("View.ErrorList");
        }

        public bool HasErrors()
        {
            CheckThread();
            return Snapshots.Count > 0;
        }

        public bool HasJsJsxErrors()
        {
            foreach (KeyValuePair<string, TableEntriesSnapshot> item in Snapshots)
            {
                if (IsJsOrJsxFile(item.Key)) return true;
            }
            return false;
        }

        private bool IsJsOrJsxFile(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToUpperInvariant();
            return extension == ".JS" || extension == ".JSX";
        }

        public bool HasErrors(string fileName)
        {
            CheckThread();
            return Snapshots.ContainsKey(fileName);
        }
    }
}
