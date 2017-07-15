using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLinterVsix.Helpers
{
    public static class TsconfigLocations
    {
        public static Tsconfig FindFromProjectItem(string projectItemFullPath)
        {
            DirectoryInfo folder = Directory.GetParent(projectItemFullPath);
            while (folder != null)
            {
                foreach (FileInfo fileInfo in folder.EnumerateFiles())
                {
                    if (LintableFiles.IsLintableTsconfig(fileInfo.FullName))
                        return new Tsconfig(fileInfo.FullName);
                }
                folder = folder.Parent;
            }
            return null;
        }

        // Helper method for FindFromSelectedItems
        private static IEnumerable<Tsconfig> FindFromProjectItemEnumerable(string projectItemFullPath)
        {
            Tsconfig tsconfig = FindFromProjectItem(projectItemFullPath);
            if (tsconfig != null) yield return tsconfig;
        }

        public static IEnumerable<Tsconfig> FindInSolution(Solution solution)
        {
            foreach (Project project in solution.Projects)
            {
                foreach (Tsconfig tsconfig in FindInProject(project)) yield return tsconfig;
            }
        }

        public static IEnumerable<Tsconfig> FindInProject(Project project)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                foreach (Tsconfig tsconfig in FindInProjectItem(projectItem)) yield return tsconfig;
            }
        }

        public static IEnumerable<Tsconfig> FindInProjectItem(ProjectItem projectItem)
        {
            string fileName = projectItem.GetFullPath();
            if (LintableFiles.IsLintableTsconfig(fileName))
                yield return new Tsconfig(fileName);
            if (projectItem.ProjectItems == null) yield break;
            foreach (ProjectItem subProjectItem in projectItem.ProjectItems)
            {
                foreach (var item in FindInProjectItem(subProjectItem)) yield return item;
            }
        }

        public static string[] FindFilterFiles(UIHierarchyItem[] items)
        {
            List<string> result = new List<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                if (selItem.Object is ProjectItem item &&
                    item.GetFullPath() is string projectItemPath &&
                    LintableFiles.IsLintableTsOrTsxFile(projectItemPath))
                {
                    result.Add(projectItemPath);
                }
                else
                    return null;
            }
            return result.ToArray();
        }

        public static IEnumerable<Tsconfig> FindFromSelectedItems(UIHierarchyItem[] items)
        {
            HashSet<string> seenPaths = new HashSet<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                if (!LintableFiles.IsLintable(selItem)) continue;
                IEnumerable<Tsconfig> currentEnumerable =
                    selItem.Object is Solution solution ? FindInSolution(solution) :
                    selItem.Object is Project project ? FindInProject(project) :
                        (selItem.Object is ProjectItem item && item.GetFullPath() is string projectItemFullPath) ?
                            FindFromProjectItemEnumerable(projectItemFullPath) : null;
                if (currentEnumerable == null) continue;
                foreach (Tsconfig tsconfig in currentEnumerable)
                {
                    if (tsconfig != null && !seenPaths.Contains(tsconfig.FullName))
                    {
                        seenPaths.Add(tsconfig.FullName);
                        yield return tsconfig;
                    }
                }
            }
        }

        public static IEnumerable<string> FindPathsFromSelectedItems(UIHierarchyItem[] items)
        {
            foreach (Tsconfig tsconfig in FindFromSelectedItems(items)) yield return tsconfig.FullName;
        }
    }
}
