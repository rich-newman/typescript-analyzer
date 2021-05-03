using System;
using System.Collections.Generic;
using System.Linq;
using WebLinter;
using System.Windows;

namespace WebLinterVsix
{
    class ErrorListService
    {
        public static void ProcessLintingResults(IEnumerable<LintingResult> results, string[] fileNames,
                                                    string[] filterFileNames, bool showErrorList)
        {
            // Called on worker thread unless we're running on a build when we are on the UI thread
            bool useFilter = WebLinterPackage.Settings.UseTsConfig && filterFileNames != null;
            IEnumerable<LintingError> allErrors = useFilter ?
                results.Where(r => r.HasErrors).SelectMany(r => r.Errors)
                       .Where(e => filterFileNames.Contains(e.FileName, StringComparer.OrdinalIgnoreCase)) :
                results.Where(r => r.HasErrors).SelectMany(r => r.Errors);
            IEnumerable<string> lintedFilesWithNoErrors = useFilter ?
                filterFileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f, StringComparer.OrdinalIgnoreCase)) :
                fileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f, StringComparer.OrdinalIgnoreCase));
            UpdateErrorListDataSource(allErrors, showErrorList, lintedFilesWithNoErrors);
        }

        private static void UpdateErrorListDataSource(IEnumerable<LintingError> allErrors, 
                                                  bool showErrorList, IEnumerable<string> lintedFilesWithNoErrors)
        {
            if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
            {
                if (allErrors.Any())
                {
                    ErrorListDataSource.Instance.AddErrors(allErrors);
                    if (showErrorList)
                        ErrorListDataSource.Instance.BringToFront();
                }

                ErrorListDataSource.Instance.CleanErrors(lintedFilesWithNoErrors);
                ErrorListDataSource.Instance.RaiseErrorListChanged();
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                    (Action)(() => UpdateErrorListDataSource(allErrors, showErrorList, lintedFilesWithNoErrors)));
            }
        }
    }
}