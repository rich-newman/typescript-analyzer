// Modifications Copyright Rich Newman 2017
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using WebLinter;

namespace WebLinterVsix
{
    public class Settings : DialogPage, ISettings
    {
        public Settings()
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            // General
            IgnoreFolderNames = @"\node_modules\,\bower_components\,\typings\,\lib\,\vendor\,.min.";
            IgnoreNestedFiles = true;
            CleanErrorsOnBuild = true;
            RunOnBuild = false;
            TSLintEnable = true;
            TSLintShowErrors = false;
        }

        public override void ResetSettings()
        {
            SetDefaults();
            base.ResetSettings();
        }

        // Advanced
        [Category("Advanced settings")]
        [DisplayName("Ignore patterns")]
        [Description("A comma-separated list of strings. Any file containing one of the strings in the path will be ignored.")]
        [DefaultValue(@"\node_modules\,\bower_components\,\typings\,\lib\,\vendor\,.min.")]
        public string IgnoreFolderNames { get; set; }

        [Category("Advanced settings")]
        [DisplayName("Ignore nested files")]
        [Description("Nested files are files that are nested under other files in Solution Explorer.")]
        [DefaultValue(true)]
        public bool IgnoreNestedFiles { get; set; }

        [Category("Advanced settings")]
        [DisplayName("Clean errors on build")]
        [Description("Clean the analyzer errors from the Error List when 'Rebuild Solution' or 'Clean' is executed.")]
        [DefaultValue(true)]
        public bool CleanErrorsOnBuild { get; set; }

        [Category("Advanced settings")]
        [DisplayName("Run on build")]
        [Description("Runs the analyzer before a build.  Will cause build to fail if there are any TSLint errors in the Visual Studio Error List.  This can only happen if 'Show errors' (below) is true.")]
        [DefaultValue(false)]
        public bool RunOnBuild { get; set; }

        [Category("TS Lint")]
        [DisplayName("Enable TSLint")]
        [Description("TSLint is a linter for TypeScript files")]
        [DefaultValue(true)]
        public bool TSLintEnable { get; set; }

        [Category("TS Lint")]
        [DisplayName("Show errors")]
        [Description("Shows TSLint errors as errors in the Error List. If false TSLint errors are shown as warnings. TSLint warnings are always shown as warnings in the Error List.")]
        [DefaultValue(false)]
        public bool TSLintShowErrors { get; set; }

        [Browsable(false)]
        public bool ShowPromptToUpgrade { get; set; } = false;

        public IEnumerable<string> GetIgnorePatterns()
        {
            var raw = IgnoreFolderNames.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pattern in raw)
            {
                yield return pattern;
            }
        }
    }
}
