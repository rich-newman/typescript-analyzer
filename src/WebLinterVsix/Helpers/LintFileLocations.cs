using EnvDTE;
using System.Collections.Generic;
using System.IO;

namespace WebLinterVsix.Helpers
{
    public static class LintFileLocations
    {
        public static IEnumerable<string> FindInSolution(Solution solution)
        {
            if (solution.Projects == null) yield break;
            foreach (Project project in solution.Projects)
            {
                foreach (string path in FindInProject(project)) yield return path;
            }
        }

        public static IEnumerable<string> FindInProject(Project project)
        {
            if (project.ProjectItems == null) yield break;
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                foreach (string path in FindInProjectItem(projectItem)) yield return path;
            }
        }

        public static IEnumerable<string> FindInProjectItem(ProjectItem projectItem)
        {
            string itemPath = projectItem.GetFullPath();
            if (LintableFiles.IsLintableTsTsxJsJsxFile(itemPath))
                yield return itemPath;
            // Checking the ignore pattern here is an optimization that prevents us iterating ignored folders
            if (projectItem.ProjectItems == null || LintableFiles.ContainsIgnorePattern(itemPath)) yield break;
            foreach (ProjectItem subProjectItem in projectItem.ProjectItems)
            {
                foreach (var item in FindInProjectItem(subProjectItem)) yield return item;
            }
        }

        public static IEnumerable<string> FindPathsFromSelectedItems(UIHierarchyItem[] items)
        {
            HashSet<string> seenPaths = new HashSet<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                if (!LintableFiles.IsLintable(selItem)) continue;
                IEnumerable<string> currentEnumerable =
                    selItem.Object is Solution solution ? FindInSolution(solution) :
                    selItem.Object is Project project ? FindInProject(project) :
                        (selItem.Object is ProjectItem projectItem) ?
                            FindInProjectItem(projectItem) : null;
                if (currentEnumerable == null) continue;
                foreach (string path in currentEnumerable)
                {
                    if (!seenPaths.Contains(path))
                    {
                        seenPaths.Add(path);
                        yield return path;
                    }
                }
            }
        }
    }
}
