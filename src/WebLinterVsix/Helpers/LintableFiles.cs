using EnvDTE;
using System;
using System.Collections.Generic;
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
            // In this test we check whether what's clicked on is lintable, meaning it's an item that might contain
            // files that can be linted.  We don't actually check whether there ARE any files that can be linted associated
            // with the item.  For example, if you rightclick a solution file we check it's a solution file, but we don't 
            // check for valid .ts or .tsx files in the solution. This applies to the ignore options as well (ignore patterns, 
            // ignore nested).
            // TODO: make the right-click check if lintable files associated with the clicked item exist
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
                  (selectedItem.Object is ProjectItem item &&
                        item.GetFullPath() is string projectItemPath &&
                        IsLintableProjectItem(projectItemPath));
        }


        public static bool IsLintableProjectItem(string projectItemPath)
        {
            return IsLintableDirectory(projectItemPath) ||
                   (!WebLinterPackage.Settings.UseTsConfig && IsLintableTsOrTsxFile(projectItemPath)) ||
                   (WebLinterPackage.Settings.UseTsConfig &&
                       (IsLintableTsconfig(projectItemPath) ||
                        IsLintableTsOrTsxFile(projectItemPath, checkIgnoreOptions: false)));
        }

        public static bool IsLintableDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;
            if (!WebLinterPackage.Settings.UseTsConfig && 
                WebLinterPackage.Settings.GetIgnorePatterns().Any(p => path.Contains(p))) return false;
            // TODO Folder is not in project??  Below always returns null, so how do we check?
            //ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(path);
            return true;
        }

        public static bool IsLintableTsOrTsxFile(string fileName, bool checkIgnoreOptions = true)
        {
            // Check if filename is absolute because when debugging, script files are sometimes dynamically created.
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!LinterFactory.IsExtensionTsOrTsx(fileName)) return false;
            return IsLintableFile(fileName, checkIgnoreOptions);
        }

        public static bool IsLintableTsconfig(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!fileName.EndsWith("tsconfig.json", ignoreCase: true, culture: null)) return false;
            return IsLintableFile(fileName);
        }

        private static bool IsLintableFile(string fileName, bool checkIgnoreOptions = true)
        {
            ProjectItem item = WebLinterPackage.Dte.Solution.FindProjectItem(fileName);
            bool isInProject = item?.GetFullPath() is string;
            if (!isInProject) return false;

            if (checkIgnoreOptions)
            {
                if (WebLinterPackage.Settings == null || WebLinterPackage.Settings.GetIgnorePatterns().Any(p => fileName.Contains(p)))
                    return false;

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
            }

            return true;
        }
    }
}
