using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLinterVsix.Helpers
{
    /// <summary>
    /// Methods to find the locations of files to lint based on selections in Solution Explorer
    /// </summary>
    public static class LintFileLocations
    {
        // TODO make an enumerable: we cast to array immediately after this is called, so we're iterating multiple times for no reason
        public static List<string> GetFilePathsFromSelectedItemPaths(UIHierarchyItem[] selectedItems)
        {
            var paths = GetSelectedItemPaths(selectedItems);
            List<string> files = new List<string>();
            foreach (string path in paths)
                AddFilesInPath(path, files);
            return files;
        }

        private static IEnumerable<string> GetSelectedItemPaths(UIHierarchyItem[] items)
        {
            foreach (UIHierarchyItem selItem in items)
            {
                string path = GetSelectedItemPath(selItem);
                if (path != null) yield return path;
            }
        }

        private static string GetSelectedItemPath(UIHierarchyItem selItem)
        {
            if (selItem.Object is ProjectItem item && item.Properties?.Item("FullPath")?.Value is string file)
                return string.IsNullOrEmpty(file) ? null : file;  // I'm unconvinced it's possible for the string to be empty

            if (selItem.Object is Project project)
                return project.GetRootFolder();

            if (selItem.Object is Solution solution)
                return Path.GetDirectoryName(solution.FullName);
            return null;
        }

        public static void AddFilesInPath(string path, List<string> files)
        {
            if (Directory.Exists(path))
            {
                var children = GetFiles(path, "*.*");
                files.AddRange(children.Where(c => LinterService.IsLintableTsOrTsxFile(c)));
            }
            else if (File.Exists(path) && LinterService.IsLintableTsOrTsxFile(path))
            {
                files.Add(path);
            }
        }

        private static List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
    }
}
