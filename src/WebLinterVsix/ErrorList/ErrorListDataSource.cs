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
        void AddErrors(IEnumerable<LintingError> errors);
        void CleanErrors(IEnumerable<string> files);
        void BringToFront();
        void CleanAllErrors();
        bool HasErrors();
        bool HasErrors(string fileName);
        event EventHandler ErrorListChanged;
        void RaiseErrorListChanged();
    }

    internal class ErrorListDataSource : ITableDataSource, IErrorListDataSource
    {
        private static IErrorListDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();

        //internal static Dictionary<string, TableEntriesSnapshot> Snapshots { get; }
        //      = new Dictionary<string, TableEntriesSnapshot>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, TableEntriesSnapshot> _snapshots = 
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

        // Don't try this at home
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
                //Debug.WriteLine("ErrorListDataSource called not on UI thread");
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
            CheckThread();
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            CheckThread();
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

        public void AddErrors(IEnumerable<LintingError> errors)
        {
            CheckThread();
            if (errors == null || !errors.Any()) return;
            var cleanErrors = errors.Where(e => e != null && !string.IsNullOrEmpty(e.FileName));
            Dictionary<string, string> fileNameToProjectNameMap = CreateFileNameToProjectNameMap();
            //DebugDumpMap(fileNameToProjectNameMap);
            foreach (IGrouping<string, LintingError> error in cleanErrors.GroupBy(t => t.FileName))
            {
                fileNameToProjectNameMap.TryGetValue(error.Key, out string projectName);
                TableEntriesSnapshot snapshot = new TableEntriesSnapshot(error.Key, projectName ?? "", error);
                Snapshots[error.Key] = snapshot;
            }
            UpdateAllSinks();
        }

        // This is an optimization for the case where we lint a very large structure: we have a very large number of files
        // and errors, inside or outside of projects.  Looking up the project for each file individually involves scanning
        // the project hierarchy, so it's better I think to scan the hierarchy first and create an efficient lookup data structure.
        // We are on the UI thread
        private Dictionary<string, string> CreateFileNameToProjectNameMap()
        {
            Dictionary<string, string> fileNameToProjectNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Solution solution = WebLinterPackage.Dte.Solution;
            if (solution?.Projects == null) return fileNameToProjectNameMap;
            foreach (Project project in solution.Projects)
            {
                if (project.ProjectItems == null) continue;
                string projectName = project.Name;
                foreach (ProjectItem projectItem in project.ProjectItems)
                    FindProjectItems(projectItem, projectName, fileNameToProjectNameMap);
            }
            return fileNameToProjectNameMap;
        }

        private void FindProjectItems(ProjectItem projectItem, string projectName, Dictionary<string, string> fileNameToProjectNameMap)
        {
            // We can't use ignore paths here as they don't apply for tsconfig results
            if (projectItem.GetFullPath() is string fileName)
            {
                // It's possible for the same file to be in two VS projects, linked in to either, issue #20
                if (Linter.IsLintableFileExtension(fileName, WebLinterPackage.Settings.LintJsFiles)
                    && !fileNameToProjectNameMap.ContainsKey(fileName))
                {
                    fileNameToProjectNameMap.Add(fileName, projectName);
                }
                if (projectItem.ProjectItems == null) return;
                foreach (ProjectItem subProjectItem in projectItem.ProjectItems)
                    FindProjectItems(subProjectItem, projectName, fileNameToProjectNameMap);
            }
        }

        [Conditional("DEBUG")]
        private void DebugDumpMap(Dictionary<string, string> map)
        {
            foreach (var item in map)
                Debug.WriteLine(item.Key + ":" + item.Value);
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

        public bool HasErrors(string fileName)
        {
            CheckThread();
            return Snapshots.ContainsKey(fileName);
        }

        public event EventHandler ErrorListChanged;
        public void RaiseErrorListChanged()
        {
            CheckThread();
            ErrorListChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
