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
        public static List<string> GetFilePathsFromSelectedItemPaths(UIHierarchyItem[] selectedItems)
        {
            var paths = GetSelectedItemPaths(selectedItems);
            List<string> files = new List<string>();
            foreach (string path in paths)
                AddLintableFilesInPath(path, files);
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
            if (selItem.Object is ProjectItem item && item.GetFullPath() is string file)
                return string.IsNullOrEmpty(file) ? null : file;  // I'm unconvinced it's possible for the string to be empty

            if (selItem.Object is Project project)
                return project.GetRootFolder();

            if (selItem.Object is Solution solution)
                return Path.GetDirectoryName(solution.FullName);
            return null;
        }

        public static void AddLintableFilesInPath(string path, List<string> files)
        {
            System.Diagnostics.Debug.WriteLine("AddLintableFilesInPath: " + path);
            if (LintableFiles.IsLintableDirectory(path))
            {
                foreach (string filePath in Directory.GetFiles(path, "*.ts?", SearchOption.TopDirectoryOnly))
                    AddLintableFilesInPath(filePath, files);
                foreach (string directoryPath in Directory.GetDirectories(path))
                    AddLintableFilesInPath(directoryPath, files);
            }
            else if (LintableFiles.IsLintableTsOrTsxFile(path))
            {
                files.Add(path);
            }
        }

        //// Replaced two methods below with one above, which is cleaner and ignores anything in a 
        //// directory which isn't lintable: previously we'd iterate over all folders and files in node_modules
        //// even if it was ignored, checking if 'node_modules' was in the path (Which it always was of course)
        //public static void AddLintableFilesInPath(string path, List<string> files)
        //{
        //    System.Diagnostics.Debug.WriteLine("AddLintableFilesInPath: " + path);
        //    //if (path.Contains("node_modules")) return;
        //    if (Directory.Exists(path))
        //    {
        //        var children = GetFiles(path, "*.ts?");
        //        files.AddRange(children.Where(c => LintableFiles.IsLintableTsOrTsxFile(c)));
        //    }
        //    else if (File.Exists(path) && LintableFiles.IsLintableTsOrTsxFile(path))
        //    {
        //        files.Add(path);
        //    }
        //}

        //private static List<string> GetFiles(string path, string pattern)
        //{
        //    System.Diagnostics.Debug.WriteLine("GetFiles: " + path);
        //    var files = new List<string>();
        //    //if (path.Contains("node_modules")) return files;

        //    try
        //    {
        //        files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
        //        foreach (var directory in Directory.GetDirectories(path))
        //            files.AddRange(GetFiles(directory, pattern));
        //    }
        //    catch (UnauthorizedAccessException) { }

        //    return files;
        //}
    }
}
