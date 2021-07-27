using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebLinterVsix.Helpers
{
    // Currently finding tsconfigs can get confusing, because there are broadly two scenarios:
    // 1. We open/save/request a lint on a single .ts file
    // 2. We request a lint on a project or solution from Solution Explorer
    // In case 1 we have to try to find an associated tsconfig.json to use for type rules: we search the folder and parent folders.
    // We lint with the first tsconfig.json we find and filter the results to the individual file requested.
    // In case 2 we find all tsconfig.jsons in the project or solution, lint with them, and show all results.
    // Case 1 means there's no real link between VS projects and tsconfig.json projects, except we do insist any tsconfigs we want to
    // lint with are included in a project somewhere in the solution.
    // Other possibilities include requesting a lint with a tsconfig.json, where we do just that and show all results, and requesting a
    // lint on a folder in Solution Explorer.  Here again we should find all tsconfigs in the folder or below and lint.
    public static class TsconfigLocations
    {
        // Given a path to a file finds any lintable tsconfig.json in the folder of the path, or any parent folder
        // 'lintable' means 'exists and is in a VS project in this solution'
        public static string FindParentTsconfig(string projectItemFullPath)
        {
            if (LintableFiles.IsLintableTsconfig(projectItemFullPath)) return projectItemFullPath;
            DirectoryInfo folder = Directory.GetParent(projectItemFullPath);
            while (folder != null)
            {
                foreach (FileInfo fileInfo in folder.EnumerateFiles())
                {
                    if (LintableFiles.IsLintableTsconfig(fileInfo.FullName))
                        return fileInfo.FullName;
                }
                folder = folder.Parent;
            }
            return null;
        }

        public static string[] FindPathsFromSelectedItems(UIHierarchyItem[] items, out Dictionary<string, string> fileToProjectMap)
        {
            fileToProjectMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> tsconfigFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UIHierarchyItem selItem in items)
            {
                if (!LintableFiles.IsLintable(selItem)) continue;
                if (selItem.Object is Solution solution)
                    FindTsconfigsInSolution(solution, tsconfigFiles, fileToProjectMap);
                else if (selItem.Object is Project project)
                    FindTsconfigsInProject(project, tsconfigFiles, fileToProjectMap);
                else if (selItem.Object is ProjectItem item && item.GetFullPath() is string projectItemPath)
                    FindTsconfigsFromSelectedProjectItem(projectItemPath, item, tsconfigFiles, fileToProjectMap);
            }
            return tsconfigFiles.ToArray();
        }

        private static void FindTsconfigsFromSelectedProjectItem(string projectItemPath, ProjectItem item, HashSet<string> result,
                                                         Dictionary<string, string> fileToProjectMap)
        {
            if (LintableFiles.IsLintableTsTsxJsJsxFile(projectItemPath))
            {
                if (!fileToProjectMap.ContainsKey(projectItemPath)) fileToProjectMap.Add(projectItemPath, item.ContainingProject.Name);
                string tsconfig = FindParentTsconfig(projectItemPath);
                if (tsconfig != null && !result.Contains(tsconfig)) result.Add(tsconfig);
            }
            else if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder || LintableFiles.IsLintableTsconfig(projectItemPath))
            {
                FindTsConfigsInProjectItem(item, result, fileToProjectMap);
            }
        }

        internal static void FindTsconfigsInSolution(Solution solution, HashSet<string> result, Dictionary<string, string> fileToProjectMap)
        {
            if (solution.Projects == null) return;
            foreach (Project project in solution.Projects)
                FindTsconfigsInProject(project, result, fileToProjectMap);
        }

        internal static void FindTsconfigsInProject(Project project, HashSet<string> result, Dictionary<string, string> fileToProjectMap)
        {
            if (project.ProjectItems == null) return;
            foreach (ProjectItem projectItem in project.ProjectItems)
                FindTsConfigsInProjectItem(projectItem, result, fileToProjectMap);
        }

        private static void FindTsConfigsInProjectItem(ProjectItem projectItem, HashSet<string> result,
                                                       Dictionary<string, string> fileToProjectMap)
        {
            string itemPath = projectItem.GetFullPath();
            if (LintableFiles.IsLintableTsTsxJsJsxFile(itemPath) && !fileToProjectMap.ContainsKey(itemPath))
                fileToProjectMap.Add(itemPath, projectItem.ContainingProject.Name);
            if (LintableFiles.IsLintableTsconfig(itemPath) && !result.Contains(itemPath)) result.Add(itemPath);
            // A project item can be a folder or a nested file, so we may need to continue searching down the tree
            if (projectItem.ProjectItems == null || LintableFiles.ContainsIgnorePattern(itemPath)) return;
            foreach (ProjectItem subProjectItem in projectItem.ProjectItems)
                FindTsConfigsInProjectItem(subProjectItem, result, fileToProjectMap);
        }

    }
}
