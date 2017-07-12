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
            UseTsConfig = false;
            RunOnOpenOrSave = true;
        }

        public override void ResetSettings()
        {
            SetDefaults();
            base.ResetSettings();
        }

        // Advanced
        [Category("Ignore")]
        [DisplayName("Ignore patterns")]
        [Description("A comma-separated list of strings without quotes. Any file containing one of the strings in the path will be ignored.")]
        [DefaultValue(@"\node_modules\,\bower_components\,\typings\,\lib\,\vendor\,.min.")]
        public string IgnoreFolderNames { get; set; }

        [Category("Ignore")]
        [DisplayName("Ignore nested files")]
        [Description("Nested files are files that are nested under other files in Solution Explorer.")]
        [DefaultValue(true)]
        public bool IgnoreNestedFiles { get; set; }

        [Category("Build")]
        [DisplayName("Clean errors on build")]
        [Description("Clean the analyzer errors from the Error List when 'Rebuild Solution' or 'Clean' is executed.")]
        [DefaultValue(true)]
        public bool CleanErrorsOnBuild { get; set; }

        [Category("Build")]
        [DisplayName("Run on build")]
        [Description("Runs the analyzer before a build.  Will cause build to fail if there are any TSLint errors in the Visual Studio Error List.  This can only happen if 'Show errors' (below) is true.")]
        [DefaultValue(false)]
        public bool RunOnBuild { get; set; }

        [Category("Basic")]
        [DisplayName("Enable TSLint")]
        [Description("TSLint is a linter for TypeScript files.")]
        [DefaultValue(true)]
        public bool TSLintEnable { get; set; }

        [Category("Basic")]
        [DisplayName("Show errors")]
        [Description("Shows TSLint errors as errors in the Error List. If false TSLint errors are shown as warnings. TSLint warnings are always shown as warnings in the Error List.")]
        [DefaultValue(false)]
        public bool TSLintShowErrors { get; set; }

        [Category("Basic")]
        [DisplayName("Use tsconfig.json files")]
        [Description("Searches for tsconfig.json files included in the Visual Studio project file, and lints using the configuration in those.")]
        [DefaultValue(false)]
        public bool UseTsConfig { get; set; }

        [Category("Basic")]
        [DisplayName("Run on file open or save")]
        [Description("Runs the analyzer for an individual file whenever it is opened or saved.")]
        [DefaultValue(true)]
        public bool RunOnOpenOrSave { get; set; }

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
