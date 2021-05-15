using System;
using System.Collections.Generic;
using System.Linq;
using WebLinter;
using System.Windows;

namespace WebLinterVsix
{
    class ErrorListService
    {
        public static void ProcessLintingResults(LintingResult result, string[] fileNames,
                                                    string[] filterFileNames, bool showErrorList, bool isFixing)
        {
            // Called on worker thread unless we're running on a build when we are on the UI thread
            // We have three possibilities re files and filters:
            // 1.  We're not using tsconfig.  In this case fileNames is the list of files we're linting, filterFileNames is null.
            // 2.  We're using tsconfig but have selected an individual file or files to lint in Solution Explorer or are saving/opening an
            //     individual file. In this case fileNames contains the relevant tsconfig(s) to pass to TSLint, and filterFileNames
            //     contains the names of the files we are linting.  These are the only errors we want to update.
            // 3.  We're using tsconfig and have selected a project/solution to lint in Solution Explorer.  In this case fileNames again
            //     contains the tsconfig file names we will pass to TSLint.  filterFileNames is again null.  We want to update all errors
            //     for the project/solution.
            bool useFilter = WebLinterPackage.Settings.UseTsConfig && filterFileNames != null; // Case 2
            bool tsConfigNoFilter = WebLinterPackage.Settings.UseTsConfig && filterFileNames == null;  // Case 3
            IEnumerable<LintingError> allErrors = useFilter ?
                result.Errors.Where(e => filterFileNames.Contains(e.FileName, StringComparer.OrdinalIgnoreCase)) :
                result.Errors;
            // lintedFilesWithNoErrors is used to clear previous errors for files with no errors remaining in cases 1 and 2
            IEnumerable<string> lintedFilesWithNoErrors = tsConfigNoFilter ? Enumerable.Empty<string>() :
                useFilter ? filterFileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f, StringComparer.OrdinalIgnoreCase)) :
                fileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f, StringComparer.OrdinalIgnoreCase));
            UpdateErrorListDataSource(allErrors, showErrorList, lintedFilesWithNoErrors, isFixing, tsConfigNoFilter);
        }

        private static void UpdateErrorListDataSource(IEnumerable<LintingError> allErrors, bool showErrorList,
                                                      IEnumerable<string> lintedFilesWithNoErrors, bool isFixing, bool tsConfigNoFilter)
        {
            if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
            {
                if (tsConfigNoFilter) ErrorListDataSource.Instance.CleanAllErrors();
                if (allErrors.Any())
                {
                    ErrorListDataSource.Instance.AddErrors(allErrors);
                    if (showErrorList)
                        ErrorListDataSource.Instance.BringToFront();
                }

                if (!tsConfigNoFilter) ErrorListDataSource.Instance.CleanErrors(lintedFilesWithNoErrors);
                WebLinterPackage.TaggerProvider?.RefreshTags(isFixing);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                    (Action)(() => UpdateErrorListDataSource(allErrors, showErrorList, lintedFilesWithNoErrors, isFixing, tsConfigNoFilter)));
            }
        }
    }
}