using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WebLinterVsix.Helpers
{
    public static class TsconfigLocations
    {
        public static Tsconfig FindFromProjectItem(string projectItemFullPath, Solution openSolution)
        {
            bool validItem = Directory.Exists(projectItemFullPath) || openSolution.FindProjectItem(projectItemFullPath) != null;
            if (!validItem) return null;
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

        public static IEnumerable<Tsconfig> FindFromSelectedItems(Array items, Solution openSolution)
        {
            HashSet<string> seenPaths = new HashSet<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                IEnumerable<Tsconfig> currentEnumerable = null;
                if (selItem.Object is ProjectItem item && item.Properties != null)
                {
                    string projectItemFullPath = item.Properties?.Item("FullPath")?.Value?.ToString();
                    if (!string.IsNullOrEmpty(projectItemFullPath))
                        currentEnumerable = FindFromProjectItemEnumerable(projectItemFullPath, openSolution);
                }
                if (selItem.Object is Project project) currentEnumerable = FindInProject(project, openSolution);
                if (selItem.Object is Solution solution) currentEnumerable = FindInSolution(solution);
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
            // Do we want to ignore nested tsconfig.jsons if we're ignoring nested files?  Don't think so
            return true;
        }

    }
}
