using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLinterVsix.Helpers
{
    /// <summary>
    /// Methods to get the locations of files to lint when building code
    /// </summary>
    public static class BuildFileLocations
    {
        public static List<string> GetBuildFilesToLint(bool isBuildingSolution, UIHierarchyItem[] selectedItems)
        {
            List<string> files = new List<string>();
            if (isBuildingSolution)
            {
                string path = GetSolutionPath();
                LintFileLocations.AddLintableFilesInPath(path, files);
            }
            else
            {
                var paths = GetSelectedItemProjectPaths(selectedItems);
                foreach (string path in paths)
                    LintFileLocations.AddLintableFilesInPath(path, files);
            }
            return files;
        }

        private static string GetSolutionPath()
        {
            Solution solution = WebLinterPackage.Dte.Solution;
            if (solution == null) return null;
            return Path.GetDirectoryName(solution.FullName);
        }

        private static IEnumerable<string> GetSelectedItemProjectPaths(UIHierarchyItem[] selectedItems)
        {
            // Note that you can build a single project from the build menu, but your options are limited
            // to the project you have selected in Solution Explorer.  Here 'project you have selected' can
            // mean the project a selected item is in.  If you ctrl-click items in two projects the menu option
            // changes to 'Build Selection', meaning build both.  This logic replicates that.
            HashSet<string> seenPaths = new HashSet<string>();
            foreach (UIHierarchyItem selectedItem in selectedItems)
            {
                Project project = selectedItem.Object is ProjectItem item ? item.ContainingProject
                                     : selectedItem.Object is Project projectItem ? projectItem : null;
                if (project?.GetRootFolder() is string projectRootFolder && !seenPaths.Contains(projectRootFolder))
                {
                    seenPaths.Add(projectRootFolder);
                    yield return projectRootFolder;
                }
                if (selectedItem.Object is Solution solution)
                    yield return Path.GetDirectoryName(solution.FullName);
            }
        }

        public static IEnumerable<string> GetTsconfigBuildFilesToLint(bool isBuildingSolution, UIHierarchyItem[] selectedItems)
        {
            IEnumerable<UIHierarchyItem> uiHierarchyItems = isBuildingSolution ? 
                                                            new UIHierarchyItem[] { GetUIHierarchySolutionItem() } :
                                                            GetSelectedItemProjectUIHierarchyItems(selectedItems);
            return TsconfigLocations.FindPathsFromSelectedItems(uiHierarchyItems.ToArray());
        }

        private static UIHierarchyItem GetUIHierarchySolutionItem()
        {
            UIHierarchyItems uiHierarchyItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems;

            foreach (UIHierarchyItem item in uiHierarchyItems)
            {
                if (item.Object is Solution) return item;
            }
            return null;
        }

        private static IEnumerable<UIHierarchyItem> GetSelectedItemProjectUIHierarchyItems(UIHierarchyItem[] selectedItems)
        {
            // Note that you can build a single project from the build menu, but your options are limited
            // to the project you have selected in Solution Explorer.  Here 'project you have selected' can
            // mean the project a selected item is in.  If you ctrl-click items in two projects the menu option
            // changes to 'Build Selection', meaning build both.  This logic replicates that.
            HashSet<string> seenPaths = new HashSet<string>();

            foreach (UIHierarchyItem selectedItem in selectedItems)
            {
                if (selectedItem.Object is ProjectItem projectItem &&
                    projectItem.ContainingProject?.GetRootFolder() is string projectItemRootFolder &&
                    !seenPaths.Contains(projectItemRootFolder))
                {
                    seenPaths.Add(projectItemRootFolder);
                    UIHierarchyItems uiHierarchyItems = WebLinterPackage.Dte.ToolWindows.SolutionExplorer.UIHierarchyItems;
                    UIHierarchyItem containingProjectHierarchyItem = GetHierarchyItemForProject(projectItemRootFolder, uiHierarchyItems);
                    if (containingProjectHierarchyItem != null) yield return containingProjectHierarchyItem;
                }
                else if (selectedItem.Object is Project project && 
                    project?.GetRootFolder() is string projectRootFolder &&
                    !seenPaths.Contains(projectRootFolder))
                {
                    seenPaths.Add(projectRootFolder);
                    yield return selectedItem;
                }
                else if (selectedItem.Object is Solution solution)
                    yield return selectedItem;
            }
        }

        private static UIHierarchyItem GetHierarchyItemForProject(string projectRootFolder, UIHierarchyItems uiHierarchyItems)
        {
            foreach (UIHierarchyItem item in uiHierarchyItems)
            {
                if (item.Object is Project project && projectRootFolder == project.GetRootFolder()) return item;
                if (item.UIHierarchyItems != null)
                {
                    UIHierarchyItem uiHierarchyItem = GetHierarchyItemForProject(projectRootFolder, item.UIHierarchyItems);
                    if (uiHierarchyItem != null) return uiHierarchyItem;
                }
            }
            return null;
        }
    }
}
