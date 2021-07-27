using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLinterVsix.Helpers
{
    public static class LintFileLocations
    {
        private static void FindInSolution(Solution solution, Dictionary<string, string> fileToProjectMap)
        {
            if (solution.Projects == null) return;
            foreach (Project project in solution.Projects)
                FindInProject(project, fileToProjectMap);
        }

        private static void FindInProject(Project project, Dictionary<string, string> fileToProjectMap)
        {
            if (project.ProjectItems == null) return;
            foreach (ProjectItem projectItem in project.ProjectItems)
                FindInProjectItem(projectItem, fileToProjectMap);
        }

        private static void FindInProjectItem(ProjectItem projectItem, Dictionary<string, string> fileToProjectMap)
        {
            string itemPath = projectItem.GetFullPath();
            if (LintableFiles.IsLintableTsTsxJsJsxFile(itemPath) && !fileToProjectMap.ContainsKey(itemPath))
                fileToProjectMap.Add(itemPath, projectItem.ContainingProject.Name);
            if (projectItem.ProjectItems == null) return;
            foreach (ProjectItem subProjectItem in projectItem.ProjectItems)
                FindInProjectItem(subProjectItem, fileToProjectMap);
        }

        public static string[] FindPathsFromSelectedItems(UIHierarchyItem[] items, out Dictionary<string, string> fileToProjectMap)
        {
            fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (UIHierarchyItem selItem in items)
            {
                if (!LintableFiles.IsLintable(selItem)) continue;
                if (selItem.Object is Solution solution)
                    FindInSolution(solution, fileToProjectMap);
                else if (selItem.Object is Project project)
                    FindInProject(project, fileToProjectMap);
                else if (selItem.Object is ProjectItem item)
                    FindInProjectItem(item, fileToProjectMap);
            }
            return fileToProjectMap.Keys.ToArray();
        }
    }
}
