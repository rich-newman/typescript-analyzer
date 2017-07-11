// Modifications Copyright Rich Newman 2017
using System.Collections.Generic;

namespace WebLinter
{
    public interface ISettings
    {
        bool IgnoreNestedFiles { get; }
        bool CleanErrorsOnBuild { get; }
        bool RunOnBuild { get; }
        bool TSLintEnable { get; }
        bool TSLintShowErrors { get; }
        bool TSLintUseTSConfig { get; }
        IEnumerable<string> GetIgnorePatterns();
        void ResetSettings();
    }
}
