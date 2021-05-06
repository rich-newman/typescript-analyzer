﻿using System.Collections.Generic;
using System.IO;
using WebLinter;

namespace WebLinterTest
{
    public class MockSettings : ISettings
    {
        private static MockSettings _settings;

        public static MockSettings Instance
        {
            get
            {
                if (_settings == null)
                    _settings = new MockSettings();

                return _settings;
            }
        }

        public static string CWD
        {
            get { return new FileInfo("../../artifacts/").FullName; }
        }

        public bool TSLintEnable => true;
        public bool TSLintShowErrors => false;
        public bool UseTsConfig { get; set; } = false;
        public bool UseProjectNGLint => false;
        public bool LintJsFiles => false;
        public bool OnlyRunIfRequested => true;
        public bool RunOnBuild => false;
        public bool CleanErrorsOnBuild => false;
        public bool IgnoreNestedFiles { get; set; } = true;
        public string[] IgnorePatterns { get; set; } = new string[0];
        public bool ShowUnderlining => true;

        public IEnumerable<string> GetIgnorePatterns() { return IgnorePatterns; }
        public void ResetSettings() { }
    }
}
