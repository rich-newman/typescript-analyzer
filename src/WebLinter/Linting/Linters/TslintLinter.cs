// Modifications Copyright Rich Newman 2017
using Newtonsoft.Json.Linq;

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
            bool hasVSErrors = false;
            foreach (JObject obj in array)
            {
                string fileName = obj["name"]?.Value<string>().Replace("/", "\\");

                if (string.IsNullOrEmpty(fileName))
                    continue;

                var le = new LintingError(fileName);
                le.Message = obj["failure"]?.Value<string>();
                le.LineNumber = obj["startPosition"]?["line"]?.Value<int>() ?? 0;
                le.ColumnNumber = obj["startPosition"]?["character"]?.Value<int>() ?? 0;
                le.IsError = Settings.TSLintShowErrors ? 
                    obj["ruleSeverity"]?.Value<string>() == "ERROR" : false;
                hasVSErrors = hasVSErrors || le.IsError;
                le.ErrorCode = obj["ruleName"]?.Value<string>();
                le.HelpLink = $"https://palantir.github.io/tslint/rules/{le.ErrorCode}";
                le.Provider = this;
                Result.Errors.Add(le);
            }
            Result.HasVsErrors = hasVSErrors;
        }
    }
}
