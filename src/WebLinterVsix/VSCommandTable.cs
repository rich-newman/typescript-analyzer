namespace WebLinterVsix
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        public const string guidVSPackageString = "4E737333-D498-4553-8096-B2F6FFF930A2";
        public const string WebLinterCmdSetString = "214001AC-2BD3-474A-A56C-B45DCB785E96";
        public const string ConfigFileCmdSetString = "03997E40-2A1E-4241-AD40-E9296C0A58A0";
        public static Guid guidVSPackage = new Guid(guidVSPackageString);
        public static Guid WebLinterCmdSet = new Guid(WebLinterCmdSetString);
        public static Guid ConfigFileCmdSet = new Guid(ConfigFileCmdSetString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        public const int ContextMenuGroup = 0x1020;
        public const int LintFilesCommand = 0x0100;
        public const int CleanErrorsCommand = 0x0200;
        public const int ToolsGroup = 0x1010;
        public const int ToolsMenu = 0x1020;
        public const int ToolsMenuGroup = 0x1030;
        public const int ToolsMenuResetGroup = 0x1040;
        public const int ResetConfigFiles = 0x0010;
        public const int EditTSLint = 0x0400;
    }
}
