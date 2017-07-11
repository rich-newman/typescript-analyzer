// Modifications Copyright Rich Newman 2017
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace WebLinter
{
    internal class TsLintLinter : LinterBase
    {
        public TsLintLinter(ISettings settings, bool fixErrors) : base(settings, fixErrors)
        {
            Name = "TSLint";
            ConfigFileName = "tslint.json";
            IsEnabled = Settings.TSLintEnable;
        }

        protected override void ParseErrors(string output)
        {
            JArray array;
            try
            {
                array = JArray.Parse(output);
            }
            catch (System.Exception ex)
            {
                //string message = "Unable to parse output from tslint. List of linting errors is expected.\r\n";
                //message += "tslint Output: " + output + "\r\n";
                //message += "Parsing Exception: " + ex.Message;
                string message = $@"Unable to parse output from tslint. List of linting errors is expected.
tslint Output: {output}
Parsing Exception: {ex.Message}";
                throw new System.FormatException(message, ex);
            }
            HashSet<LintingError> seen = new HashSet<LintingError>();
            bool hasVSErrors = false;
            foreach (JObject obj in array)
            {
                string fileName = obj["name"]?.Value<string>().Replace("/", "\\");
                if (string.IsNullOrEmpty(fileName)) continue;

                int lineNumber = obj["startPosition"]?["line"]?.Value<int>() ?? 0;
                int columnNumber = obj["startPosition"]?["character"]?.Value<int>() ?? 0;
                bool isError = Settings.TSLintShowErrors ?
                    obj["ruleSeverity"]?.Value<string>() == "ERROR" : false;
                hasVSErrors = hasVSErrors || isError;
                string errorCode = obj["ruleName"]?.Value<string>();

                LintingError le = new LintingError(fileName, lineNumber, columnNumber, isError, errorCode);
                if (!Result.Errors.Contains(le))
                {
                    le.Message = obj["failure"]?.Value<string>();
                    le.HelpLink = $"https://palantir.github.io/tslint/rules/{le.ErrorCode}";
                    le.Provider = this;
                    Result.Errors.Add(le);
                    seen.Add(le);
                }
            }
            Result.HasVsErrors = hasVSErrors;
        }
    }
}
