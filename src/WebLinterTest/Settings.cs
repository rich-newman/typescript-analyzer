// Modifications Copyright Rich Newman 2017
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using WebLinter;

namespace WebLinterTest
{
    class Settings : ISettings
    {
        private static Settings _settings;

        public static Settings Instance
        {
            get
            {
                if (_settings == null)
                    _settings = new Settings();

                return _settings;
            }
        }

        public static string CWD
        {
            get { return new FileInfo("../../artifacts/").FullName; }
        }

        public bool TSLintEnable => true;
        public bool TSLintShowErrors => false;
        public bool TSLintUseTSConfig => true;
        public bool RunOnBuild => false;
        public bool CleanErrorsOnBuild => false;
        public bool IgnoreNestedFiles => true;
        public IEnumerable<string> GetIgnorePatterns() { return new string[0]; }
        public void ResetSettings() { }
    }
}
