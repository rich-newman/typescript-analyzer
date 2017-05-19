using System.Collections.Generic;
using System.Linq;
using WebLinter;

namespace WebLinterVsix
{
    class ErrorListService
    {
        public static void ProcessLintingResults(IEnumerable<LintingResult> results, string[] fileNames, bool showErrorList)
        {
            IEnumerable<LintingError> errors = results.Where(r => r.HasErrors).SelectMany(r => r.Errors);
            IEnumerable<string> clean = fileNames.Where(f => !errors.Select(e => e.FileName).Contains(f));

            if (errors.Any())
            {
                TableDataSource.Instance.AddErrors(errors);
                if (showErrorList)
                    TableDataSource.Instance.BringToFront();
            }

            TableDataSource.Instance.CleanErrors(clean);
        }
    }
}
