using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLinterVsix.Helpers
{
    public static class TsconfigLocations
    {
        public static Tsconfig FindFromProjectItem(string projectItemFullPath, Solution openSolution)
        {
            DirectoryInfo folder = Directory.GetParent(projectItemFullPath);
            while (folder != null)
            {
                foreach (FileInfo fileInfo in folder.EnumerateFiles())
                {
                    if (IsValidTsconfig(fileInfo.FullName, openSolution))
                        return new Tsconfig(fileInfo.FullName);
                }
                folder = folder.Parent;
            }
            return null;
        }

        // Helper method for FindFromSelectedItems
        private static IEnumerable<Tsconfig> FindFromProjectItemEnumerable(string projectItemFullPath, Solution openSolution)
        {
            Tsconfig tsconfig = FindFromProjectItem(projectItemFullPath, openSolution);
            if (tsconfig != null) yield return tsconfig;
        }

        public static IEnumerable<Tsconfig> FindInSolution(Solution solution)
        {
            foreach (Project project in solution.Projects)
            {
                foreach (Tsconfig tsconfig in FindInProject(project, solution)) yield return tsconfig;
            }
        }

        public static IEnumerable<Tsconfig> FindInProject(Project project, Solution openSolution)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                foreach (Tsconfig tsconfig in FindInProjectItem(projectItem, openSolution)) yield return tsconfig;
            }
        }

        public static IEnumerable<Tsconfig> FindInProjectItem(ProjectItem projectItem, Solution openSolution)
        {
            string fileName = projectItem.Properties?.Item("FullPath")?.Value?.ToString();
            if (fileName != null && IsValidTsconfig(fileName, openSolution))
                yield return new Tsconfig(fileName);
            if (projectItem.ProjectItems == null) yield break;
            foreach (ProjectItem subProjectItem in projectItem.ProjectItems)
            {
                foreach (var item in FindInProjectItem(subProjectItem, openSolution)) yield return item;
            }
        }

        public static string[] FindFilterFiles(UIHierarchyItem[] items, Solution openSolution)
        {
            List<string> result = new List<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                if (selItem.Object is ProjectItem item &&
                    item.Properties?.Item("FullPath")?.Value is string projectItemPath &&
                    LinterService.IsLintableTsOrTsxFile(projectItemPath))
                {
                    result.Add(projectItemPath);
                }
                else
                    return null;
            }
            return result.ToArray();
        }

        public static IEnumerable<Tsconfig> FindFromSelectedItems(UIHierarchyItem[] items, Solution openSolution)
        {
            HashSet<string> seenPaths = new HashSet<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                if (!LinterService.IsLintable(selItem)) continue;
                IEnumerable<Tsconfig> currentEnumerable =
                    selItem.Object is Solution solution ? FindInSolution(solution) :
                    selItem.Object is Project project ? FindInProject(project, openSolution) :
                        (selItem.Object is ProjectItem item && item.Properties != null &&
                        item.Properties?.Item("FullPath")?.Value is string projectItemFullPath) ?
                            FindFromProjectItemEnumerable(projectItemFullPath, openSolution) : null;
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

        public static IEnumerable<string> FindPathsFromSelectedItems(UIHierarchyItem[] items, Solution openSolution)
        {
            foreach (Tsconfig tsconfig in FindFromSelectedItems(items, openSolution)) yield return tsconfig.FullName;
        }

        public static bool IsValidTsconfig(string fileName, Solution openSolution, bool checkIfInSolution = true)
        {
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(fileName)) return false;
            if (!fileName.EndsWith("tsconfig.json", true, null)) return false;
            IEnumerable<string> patterns = WebLinterPackage.Settings.GetIgnorePatterns();
            if (patterns.Any(p => fileName.Contains(p))) return false;
            if (checkIfInSolution)
            {
                ProjectItem projectItem = openSolution.FindProjectItem(fileName);
                if (projectItem == null) return false;
            }
            return true;
        }

    }
}
