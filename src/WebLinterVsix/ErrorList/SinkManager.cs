using Microsoft.VisualStudio.Shell.TableManager;
using System.Linq;
using System;
using System.Collections.Generic;
using WebLinter;

namespace WebLinterVsix
{
    class SinkManager : IDisposable
    {
        private readonly ITableDataSink _sink;
        private TableDataSource _errorList;
        private List<TableEntriesSnapshot> _snapshots = new List<TableEntriesSnapshot>();

        internal SinkManager(TableDataSource errorList, ITableDataSink sink)
        {
            _sink = sink;
            _errorList = errorList;

            errorList.AddSinkManager(this);
        }

        internal void Clear()
        {
            _sink.RemoveAllSnapshots();
        }

        internal void UpdateSink(IEnumerable<TableEntriesSnapshot> snapshots)
        {
            try
            {
                foreach (var snapshot in snapshots)
                {
                    var existing = _snapshots.FirstOrDefault(s => snapshot.FilePath.Equals(s.FilePath, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        _snapshots.Remove(existing);
                        _sink.ReplaceSnapshot(existing, snapshot);
                    }
                    else
                    {
                        _sink.AddSnapshot(snapshot);
                    }

                    _snapshots.Add(snapshot);
                }

            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        internal void RemoveSnapshots(IEnumerable<string> files)
        {
            try
            {
                foreach (string file in files)
                {
                    var existing = _snapshots.FirstOrDefault(s => file.Equals(s.FilePath, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        _snapshots.Remove(existing);
                        _sink.RemoveSnapshot(existing);
                    }
                }
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        public void Dispose()
        {
            // Called when the person who subscribed to the data source disposes of the cookie (== this object) they were given.
            _errorList.RemoveSinkManager(this);
        }
    }
}
