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

        public static IEnumerable<string> GetSelectedItemPaths()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                ProjectItem item = selItem.Object as ProjectItem;

                if (item != null && item.Properties != null)
                {
                    string file= item.Properties.Item("FullPath").Value.ToString();

                    if (!string.IsNullOrEmpty(file))
                        yield return file;
                    else
                        continue;
                }

                Project project = selItem.Object as Project;

                if (project != null)
                    yield return project.GetRootFolder();

                // We can iterate over all the contained projects or we
                // can just look at everything below the solution file
                Solution solution = selItem.Object as Solution;
                if (solution != null)
                    yield return Path.GetDirectoryName(solution.FullName);
            }
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
