using System.Collections.Generic;
using System.Linq;
using WebLinter;

namespace WebLinterVsix
{
    class ErrorListService
    {
        private static object _processLintingLocker = new object();
        public static void ProcessLintingResults(IEnumerable<LintingResult> results, string[] fileNames,
                                                    string[] filterFileNames, bool showErrorList)
        {
            bool useFilter = WebLinterPackage.Settings.UseTsConfig && filterFileNames != null;
            IEnumerable<LintingError> allErrors = useFilter ?
                results.Where(r => r.HasErrors).SelectMany(r => r.Errors).Where(e => filterFileNames.Contains(e.FileName)) :
                results.Where(r => r.HasErrors).SelectMany(r => r.Errors);
            IEnumerable<string> lintedFilesWithNoErrors = useFilter ?
                filterFileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f)) :
                fileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f));
            lock (_processLintingLocker)
            {
                if (allErrors.Any())
                {
                    TableDataSource.Instance.AddErrors(allErrors);
                    if (showErrorList)
                        TableDataSource.Instance.BringToFront();
                }

                TableDataSource.Instance.CleanErrors(lintedFilesWithNoErrors);
                TableDataSource.Instance.RaiseErrorListChanged();
            }
        }
    }
}