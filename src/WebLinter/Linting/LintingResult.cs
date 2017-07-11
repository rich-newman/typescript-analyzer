using System.Collections.Generic;
using System.Linq;

namespace WebLinter
{
    public class LintingResult
    {
        public LintingResult(params string[] fileNames)
        {
            FileNames.AddRange(fileNames);
        }

        public List<string> FileNames { get; set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;

        public HashSet<LintingError> Errors { get; } = new HashSet<LintingError>();

        // HasVsErrors is true iff there exists a TSLint error that will be
        // displayed as a Visual Studio error in the Error Window
        public bool HasVsErrors { get; set; }
    }
}