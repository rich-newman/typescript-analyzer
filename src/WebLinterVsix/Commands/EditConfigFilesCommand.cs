﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using Microsoft.VisualStudio.Shell;
using WebLinter;

namespace WebLinterVsix
{
    internal sealed class EditConfigFilesCommand
    {
        private readonly Package _package;

        private EditConfigFilesCommand(Package package)
        {
            _package = package;

            List<CommandID> list = new List<CommandID>
            {
                new CommandID(PackageGuids.ConfigFileCmdSet, PackageIds.EditTSLint),
            };

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            foreach (var id in list)
            {
                var menuItem = new MenuCommand(EditConfig, id);
                commandService.AddCommand(menuItem);
            }
        }

        public static EditConfigFilesCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new EditConfigFilesCommand(package);
        }

        private void EditConfig(object sender, EventArgs e)
        {
            try
            {
                var button = (MenuCommand)sender;

                string folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string fileName = GetFileName(button.CommandID.ID);
                string configFile = Path.Combine(folder, fileName);

                if (!string.IsNullOrEmpty(configFile) && File.Exists(configFile))
                    WebLinterPackage.Dte.ExecuteCommand("File.OpenFile", "\"" + configFile + "\"");
                else
                    WebLinterPackage.Dte.StatusBar.Text = $"Configuration file not found: {configFile}";
            }
            catch (Exception ex) { Logger.LogAndWarn(ex); }
        }

        private string GetFileName(int commandId)
        {
            switch (commandId)
            {
                case PackageIds.EditTSLint:
                    return "tslint.json";
            }

            return null;
        }
    }
}
