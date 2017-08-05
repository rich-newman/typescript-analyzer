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
    internal interface IErrorsTableDataSource
    {
        void AddErrors(IEnumerable<LintingError> errors);
        void CleanErrors(IEnumerable<string> files);
        void BringToFront();
        void CleanAllErrors();
        bool HasErrors();
        bool HasErrors(string fileName);
    }

    internal class TableDataSource : ITableDataSource, IErrorsTableDataSource
    {
        private static IErrorsTableDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();
        private static Dictionary<string, TableEntriesSnapshot> _snapshots = new Dictionary<string, TableEntriesSnapshot>();

        internal static Dictionary<string, TableEntriesSnapshot> Snapshots => _snapshots;  // For unit testing only

        [Import]
        private ITableManagerProvider TableManagerProvider { get; set; } = null;

        private TableDataSource()
        {
            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

            var manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            manager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName, StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column);
        }

        // Don't try this at home
        internal static void InjectMockErrorsTableDataSource(IErrorsTableDataSource instance) => _instance = instance;

        public static IErrorsTableDataSource Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TableDataSource();

                return _instance;
            }
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
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Add(manager);
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }

        public void UpdateAllSinks()
        {
            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.UpdateSink(_snapshots.Values);
                }
            }
        }

        public void AddErrors(IEnumerable<LintingError> errors)
        {
            if (errors == null || !errors.Any())
                return;

            var cleanErrors = errors.Where(e => e != null && !string.IsNullOrEmpty(e.FileName));
            Dictionary<string, string> fileNameToProjectNameMap = CreateFileNameToProjectNameMap();
            //DebugDumpMap(fileNameToProjectNameMap);
            foreach (var error in cleanErrors.GroupBy(t => t.FileName))
            {
                fileNameToProjectNameMap.TryGetValue(error.Key, out string projectName);
                TableEntriesSnapshot snapshot = new TableEntriesSnapshot(error.Key, projectName ?? "", error);
                _snapshots[error.Key] = snapshot;
            }

            UpdateAllSinks();
        }

        // This is an optimization for the case where we lint a very large structure: we have a very large number of files
        // and errors, inside or outside of projects.  Looking up the project for each file individually involves scanning
        // the project hierarchy, so it's better I think to scan the hierarchy first and create an efficient lookup data structure.
        // We are on the UI thread
        private Dictionary<string, string> CreateFileNameToProjectNameMap()
        {
            Dictionary<string, string> fileNameToProjectNameMap = new Dictionary<string, string>();
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
                if (LinterFactory.IsExtensionTsOrTsx(fileName)) fileNameToProjectNameMap.Add(fileName, projectName);
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
            foreach (string file in files)
            {
                if (_snapshots.ContainsKey(file))
                {
                    _snapshots[file].Dispose();
                    _snapshots.Remove(file);
                }
            }

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.RemoveSnapshots(files);
                }
            }

            UpdateAllSinks();
        }

        public void CleanAllErrors()
        {
            foreach (string file in _snapshots.Keys)
            {
                var snapshot = _snapshots[file];
                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }

            _snapshots.Clear();

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.Clear();
                }
            }
        }

        public void BringToFront()
        {
            WebLinterPackage.Dte.ExecuteCommand("View.ErrorList");
        }

        public bool HasErrors()
        {
            return _snapshots.Count > 0;
        }

        public bool HasErrors(string fileName)
        {
            return _snapshots.ContainsKey(fileName);
        }
    }
}
