using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using WebLinter;

namespace WebLinterVsix.Tagging
{
    /// <summary>
    /// Wrapper for a LintingError that provides additional information that 
    /// allows underlining ('tagging') in the code window
    /// </summary>
    public class LintingErrorTag : IErrorTag
    {
        public string ErrorType { get; }
        public object ToolTipContent { get; }

        internal LintingErrorTag(LintingError lintingError)
        {
            ErrorType = lintingError.IsError ? PredefinedErrorTypeNames.SyntaxError : PredefinedErrorTypeNames.Warning;
            ToolTipContent = $"Analyzer - {lintingError.Message}";
        }
    }
}
