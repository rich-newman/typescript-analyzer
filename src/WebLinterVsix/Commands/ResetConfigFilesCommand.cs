﻿using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using WebLinter;

namespace WebLinterVsix
{
    internal sealed class ResetConfigFilesCommand
    {
        private readonly Package _package;

        private ResetConfigFilesCommand(Package package)
        {
            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            var menuCommandID = new CommandID(PackageGuids.ConfigFileCmdSet, PackageIds.ResetConfigFiles);
            var menuItem = new OleMenuCommand(ResetConfigurationFiles, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ResetConfigFilesCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new ResetConfigFilesCommand(package);
        }

        private async void ResetConfigurationFiles(object sender, EventArgs e)
        {
            try
            {
                string msg = "This will reset the configuration for the TypeScript Analyzer to its defaults.\n\nDo you wish to continue?";
                var result = MessageBox.Show(msg, Vsix.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    await LinterService.CopyResourceFilesToUserProfile(true);
                    WebLinterPackage.Settings.ResetSettings();
                    WebLinterPackage.Dte.StatusBar.Text = "TypeScript Analyzer (tslint) configuration files have been reset";
                }
            }
            catch (Exception ex)
            {
                Logger.LogAndWarn(ex);
            }

        }
    }
}
