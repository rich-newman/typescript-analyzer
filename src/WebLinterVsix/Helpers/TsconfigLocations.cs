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
        public static Tsconfig FindFromProjectItem(string projectItemFullName, Solution openSolution)
        {
            ProjectItem projectItem = openSolution.FindProjectItem(projectItemFullName);
            if (projectItem == null) return null;
            //Project project = projectItem.ContainingProject;

            DirectoryInfo folder = Directory.GetParent(projectItemFullName);
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

        // TODO may be better - needs testing
        public static IEnumerable<Tsconfig> FindFromProjectItemEnumerable(string projectItemFullName, Solution openSolution)
        {
            ProjectItem projectItem = openSolution.FindProjectItem(projectItemFullName);
            if (projectItem == null) yield break;
            //Project project = projectItem.ContainingProject;

            DirectoryInfo folder = Directory.GetParent(projectItemFullName);
            while (folder != null)
            {
                foreach (FileInfo fileInfo in folder.EnumerateFiles())
                {
                    if (IsValidTsconfig(fileInfo.FullName, openSolution))
                    {
                        yield return new Tsconfig(fileInfo.FullName);
                        yield break;
                    }
                }
                folder = folder.Parent;
            }
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
            string fileName = projectItem.Properties.Item("FullPath")?.Value?.ToString();
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
            // TODO horrible and repetitive: REFACTOR (but let's make it work first)
            // Get an IEnumerable<Tsconfig> based on input on each loop pass, and then iterate that yield returning
            // Have a seen(item, seenPaths) static method
            // TODO if there are no tsconfigs we need to fall back to the original algo: how the hell do we do that with iterators?  FirstOrDefault?
            // Handle in calling code?
            HashSet<string> seenPaths = new HashSet<string>();
            foreach (UIHierarchyItem selItem in items)
            {
                if (selItem.Object is ProjectItem item && item.Properties != null)
                {
                    string file = item.Properties.Item("FullPath").Value.ToString();
                    if (!string.IsNullOrEmpty(file))
                    {
                        Tsconfig tsconfig = FindFromProjectItem(file, openSolution);
                        if (tsconfig != null && !seenPaths.Contains(tsconfig.FullName))
                        {
                            seenPaths.Add(tsconfig.FullName);
                            yield return tsconfig;
                        }
                    }
                    else
                        continue;
                }

                if (selItem.Object is Project project)
                {
                    foreach (Tsconfig tsconfig in FindInProject(project, openSolution))
                    {
                        if (tsconfig != null && !seenPaths.Contains(tsconfig.FullName))
                        {
                            seenPaths.Add(tsconfig.FullName);
                            yield return tsconfig;
                        }
                    }
                }

                if (selItem.Object is Solution solution)
                {
                    foreach (Tsconfig tsconfig in FindInSolution(solution))
                    {
                        if (tsconfig != null && !seenPaths.Contains(tsconfig.FullName))
                        {
                            seenPaths.Add(tsconfig.FullName);
                            yield return tsconfig;
                        }
                    }
                }
            }
        }

        private static bool IsValidTsconfig(string fileName, Solution openSolution, bool checkIfInSolution = true)
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
