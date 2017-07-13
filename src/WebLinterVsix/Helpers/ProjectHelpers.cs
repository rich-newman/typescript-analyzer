using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using EnvDTE80;

namespace WebLinterVsix
{
    public static class ProjectHelpers
    {
        private static DTE2 _dte = WebLinterPackage.Dte;

        public static string GetSolutionPath()
        {
            Solution solution = _dte.Solution;
            if (solution == null) return null;
            return Path.GetDirectoryName(solution.FullName);
        }

        public static IEnumerable<string> GetSelectedItemProjectPaths()
        {
            HashSet<string> seenPaths = new HashSet<string>();
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                Project project = selItem.Object is ProjectItem item ? item.ContainingProject 
                                     : selItem.Object is Project projectItem ? projectItem : null;
                if (project != null)
                {
                    string projectRootFolder = project.GetRootFolder();
                    if (projectRootFolder != null && !seenPaths.Contains(projectRootFolder))
                    {
                        seenPaths.Add(projectRootFolder);
                        yield return projectRootFolder;
                    }
                }
                Solution solution = selItem.Object as Solution;
                if (solution != null)
                    yield return Path.GetDirectoryName(solution.FullName);
            }
        }

        public static IEnumerable<string> GetSelectedItemPaths()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                string path = GetSelectedItemPath(selItem);
                if (path != null) yield return path;
            }
        }

        public static string GetSelectedItemPath(UIHierarchyItem selItem)
        {
            ProjectItem item = selItem.Object as ProjectItem;
            if (item != null && item.Properties != null)
            {
                string file = item.Properties.Item("FullPath").Value.ToString();

                if (!string.IsNullOrEmpty(file))
                    return file;
                else
                    return null;
            }

            Project project = selItem.Object as Project;
            if (project != null)
                return project.GetRootFolder();

            Solution solution = selItem.Object as Solution;
            if (solution != null)
                return Path.GetDirectoryName(solution.FullName);
            return null;
        }

        public static string GetRootFolder(this Project project)
        {
            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }
    }
}
