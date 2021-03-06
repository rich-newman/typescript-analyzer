﻿using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;

namespace WebLinterVsix
{
    public static class Logger
    {
        private static IVsOutputWindowPane pane;
        private static WebLinterPackage _provider;
        private static string _name;

        public static void Initialize(WebLinterPackage provider, string name)
        {
            _provider = provider;
            _name = name;
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane.OutputString(System.String)")]
        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            try
            {
                if (EnsurePane())
                    pane.OutputString(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
            }
            catch { }
        }

        public static void Log(Exception ex)
        {
            try
            {
                if (ex != null) Log(ex.ToString());
            }
            catch { }
        }

        public static void LogAndWarn(Exception ex)
        {
            if (ex != null) LogAndWarn(ex.Message);
        }

        public static void LogAndWarn(string message, bool showWarning = true)
        {
            try
            {
                Log(message);
                if (showWarning)
                    WebLinterPackage.Dte.StatusBar.Text = "A TypeScript Analyzer error occurred. See Output window for more details.";
            }
            catch { }
        }

        private static bool EnsurePane()
        {
            // during unit tests, _provider is not set. Do not try to get pane then.
            if (pane == null && _provider != null)
            {
                Guid guid = Guid.NewGuid();
                IVsOutputWindow output = _provider.GetIVsOutputWindow();
                output.CreatePane(ref guid, _name, 1, 1);
                output.GetPane(ref guid, out pane);
            }

            return pane != null;
        }
    }
}