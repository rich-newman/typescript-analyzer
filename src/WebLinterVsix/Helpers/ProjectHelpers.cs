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
            // Note that you can build a single project from the build menu, but your options are limited
            // to the project you have selected in Solution Explorer.  Here 'project you have selected' can
            // mean the project a selected item is in.  If you ctrl-click items in two projects the menu option
            // changes to 'Build Selection', meaning build both.  This logic replicates that.
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

        public static IEnumerable<UIHierarchyItem> GetSelectedItemProjectUIHierarchyItems()
        {
            // Note that you can build a single project from the build menu, but your options are limited
            // to the project you have selected in Solution Explorer.  Here 'project you have selected' can
            // mean the project a selected item is in.  If you ctrl-click items in two projects the menu option
            // changes to 'Build Selection', meaning build both.  This logic replicates that.
            HashSet<string> seenPaths = new HashSet<string>();
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                ProjectItem projectItem = selItem.Object as ProjectItem;
                if(projectItem != null)
                {
                    Project containingProject = projectItem.ContainingProject;
                    if (containingProject != null)
                    {
                        string projectRootFolder = containingProject.GetRootFolder();
                        if (projectRootFolder != null && !seenPaths.Contains(projectRootFolder))
                        {
                            seenPaths.Add(projectRootFolder);
                            UIHierarchyItems uiHierarchyItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems;
                            UIHierarchyItem containingProjectHierarchyItem = GetHierarchyItemForProject(projectRootFolder, uiHierarchyItems);
                            if (containingProjectHierarchyItem != null) yield return containingProjectHierarchyItem;
                        }
                    }
                }

                Project project = selItem.Object as Project;
                if (project != null)
                {
                    string projectRootFolder = project.GetRootFolder();
                    if (projectRootFolder != null && !seenPaths.Contains(projectRootFolder))
                    {
                        seenPaths.Add(projectRootFolder);
                        yield return selItem;
                    }
                }
                Solution solution = selItem.Object as Solution;
                if (solution != null)
                    yield return selItem;
            }
        }

        private static UIHierarchyItem GetHierarchyItemForProject(string projectRootFolder, UIHierarchyItems uiHierarchyItems)
        {
            foreach (UIHierarchyItem item in uiHierarchyItems)
            {
                if (item.Object is Project project && projectRootFolder == project.GetRootFolder()) return item;
                if(item.UIHierarchyItems != null)
                {
                    UIHierarchyItem uiHierarchyItem = GetHierarchyItemForProject(projectRootFolder, item.UIHierarchyItems);
                    if (uiHierarchyItem != null) return uiHierarchyItem;
                }
            }
            return null;
        }

        public static IEnumerable<string> GetSelectedItemPaths(UIHierarchyItem[] items)
        {
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
                {
                    if (file.EndsWith("file8.ts"))
                        System.Diagnostics.Debug.WriteLine("Found");
                    return file;
                }
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
