using EnvDTE;
using System;
using System.IO;
using System.Linq;
using WebLinter;

namespace WebLinterVsix.Helpers
{
    /// <summary>
    /// Methods to test whether various items can be linted
    /// </summary>
    /// <remarks>
    /// I don't think 'lintable' is a valid adjective either.  But then neither is 'lint' as a verb. http://www.dictionary.com/browse/lint
    /// </remarks>
    public static class LintableFiles
    {
        public static bool AreAllSelectedItemsLintable()
        {
            UIHierarchyItem[] selectedItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];

            foreach (UIHierarchyItem selectedItem in selectedItems)
            {
                if (!IsLintable(selectedItem)) return false;
            }
            return true;
        }

        public static bool IsLintable(UIHierarchyItem selectedItem)
        {
            return selectedItem.Object is Solution ||
                   selectedItem.Object is Project ||
                   selectedItem.Object is EnvDTE80.SolutionFolder ||
                  (selectedItem.Object is ProjectItem item &&
                        item.GetFullPath() is string projectItemPath &&
                        IsLintableProjectItem(projectItemPath));
        }


        public static bool IsLintableProjectItem(string projectItemPath)
        {
            return IsLintableDirectory(projectItemPath) ||
                   IsLintableTsOrTsxFile(projectItemPath) ||
                   (WebLinterPackage.Settings.UseTsConfig && IsLintableTsconfig(projectItemPath));
        }

        public static bool IsLintableDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;
            if (WebLinterPackage.Settings.GetIgnorePatterns().Any(p => path.Contains(p))) return false;
            // TODO Folder is not in project??  Below always returns null, so how do we check?
            //ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(path);
            return true;
        }

        public static bool IsLintableTsOrTsxFile(string fileName)
        {
            // Check if filename is absolute because when debugging, script files are sometimes dynamically created.
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!LinterFactory.IsExtensionTsOrTsx(fileName)) return false;
            return IsLintableFile(fileName);
        }

        public static bool IsLintableTsconfig(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!fileName.EndsWith("tsconfig.json", ignoreCase: true, culture: null)) return false;
            return IsLintableFile(fileName);
        }

        private static bool IsLintableFile(string fileName)
        {
            if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.GetIgnorePatterns().Any(p => fileName.Contains(p)))
                return false;

            ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(fileName);
            bool isInProject = item?.GetFullPath() is string;
            if (!isInProject) return false;

            // Ignore nested files
            if (WebLinterPackage.Settings.IgnoreNestedFiles)
            {
                // item.Collection is not supported in Node.js projects
                if (item.ContainingProject.Kind.Equals("{9092aa53-fb77-4645-b42d-1ccca6bd08bd}", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (item.Collection?.Parent is ProjectItem parent &&
                    parent.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                    return false;
            }

            return true;
        }
    }
}
