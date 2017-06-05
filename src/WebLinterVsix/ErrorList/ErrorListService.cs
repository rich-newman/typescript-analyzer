using System.Collections.Generic;
using System.Linq;
using WebLinter;

namespace WebLinterVsix
{
    class ErrorListService
    {
        public static void ProcessLintingResults(IEnumerable<LintingResult> results, string[] fileNames, bool showErrorList)
        {
            IEnumerable<LintingError> allErrors = results.Where(r => r.HasErrors).SelectMany(r => r.Errors);
            IEnumerable<string> lintedFilesWithNoErrors = fileNames.Where(f => !allErrors.Select(e => e.FileName).Contains(f));

            if (allErrors.Any())
            {
                TableDataSource.Instance.AddErrors(allErrors);
                if (showErrorList)
                    TableDataSource.Instance.BringToFront();
            }

            TableDataSource.Instance.CleanErrors(lintedFilesWithNoErrors);
        }
    }
}
