﻿using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLinterVsix.Helpers;

namespace WebLinterTest
{
    /// <summary>
    /// Tests file discovery for linting from selected items in Solution Explorer when we are building
    /// Separate tests test the cases when use tsconfig.json is true and when it is false
    /// </summary>
    /// <remarks>
    /// The rules are slightly different from regular file discovery: if a single item or items is
    /// selected in Solution Explorer we need to figure out which VS projects are being built
    /// </remarks>
    [TestClass]
    public class BuildFileLocationsTest
    {
        private static EnvDTE80.DTE2 dte = null;
        private static EnvDTE.Solution solution = null;
        private static MockSettings settings = null;

        // Clunky, but better than nothing
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            MessageFilter.Register();
            Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
            object inst = System.Activator.CreateInstance(type, true);
            dte = (EnvDTE80.DTE2)inst;
            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/tsconfig/Tsconfig.sln"));
            solution = dte.Solution;

            settings = new MockSettings() { UseTsConfig = true };
        }

        [TestInitialize]
        public void TestInitialize()
        { 
            WebLinterVsix.WebLinterPackage.Settings = settings;
            WebLinterVsix.WebLinterPackage.Dte = dte;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WebLinterVsix.WebLinterPackage.Settings = null;
            WebLinterVsix.WebLinterPackage.Dte = null;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (solution != null) { solution.Close(); solution = null; }
            if (dte != null) dte.Quit();
            WebLinterVsix.WebLinterPackage.Settings = null;
            WebLinterVsix.WebLinterPackage.Dte = null;
            MessageFilter.Revoke();
        }

        [TestMethod, TestCategory("Build File Locations")]
        public void GetBuildFilesForSolution()
        {
            List<string> results = BuildFileLocations.GetBuildFilesToLint(isBuildingSolution: true, 
                                                                          selectedItems: null, useTsConfig: false).ToList();

            // Should be the same as if we are linting the solution
            Assert.AreEqual(9, results.Count);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
            string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
            string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");
            string expected9 = Path.GetFullPath(@"../../artifacts/tsconfig/file9.ts"); // Linked file

            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
            Assert.IsTrue(results.Contains(expected4));
            Assert.IsTrue(results.Contains(expected5));
            Assert.IsTrue(results.Contains(expected6));
            Assert.IsTrue(results.Contains(expected7));
            Assert.IsTrue(results.Contains(expected8));
            Assert.IsTrue(results.Contains(expected9));
        }

        [TestMethod, TestCategory("Build File Locations")]
        public void GetBuildFilesForProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
            Project project = TsconfigLocationsTest.FindProject(projectFullName, solution);
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = project };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            List<string> results = BuildFileLocations.GetBuildFilesToLint(isBuildingSolution: false,
                                                                          selectedItems: selectedItems, useTsConfig: false).ToList();

            Assert.AreEqual(1, results.Count);
            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file5.ts");
            Assert.IsTrue(results.Contains(expected1));
        }

        [TestMethod, TestCategory("Build File Locations")]
        public void GetBuildFilesForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = TsconfigLocationsTest.FindProject(projectFullName, solution);
            ProjectItem projectItem = TsconfigLocationsTest.FindProjectItemInProject(projectItemFullName, project);

            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = projectItem };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            List<string> results = BuildFileLocations.GetBuildFilesToLint(isBuildingSolution: false,
                                                                          selectedItems: selectedItems, useTsConfig: false).ToList();

            // We're going to build the project that the individual file is in (tsconfigTest) and so need to lint
            // all the files in said project before the build.  Note NOT using tsconfig.json (useTsConfig: false above).
            Assert.AreEqual(8, results.Count);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react-dom.d.ts");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/react.d.ts");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
            string expected4 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string expected5 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
            string expected6 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/file3.ts");
            string expected7 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file6.tsx");
            string expected8 = Path.GetFullPath(@"../../artifacts/tsconfig/file9.ts"); // Linked file

            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
            Assert.IsTrue(results.Contains(expected4));
            Assert.IsTrue(results.Contains(expected5));
            Assert.IsTrue(results.Contains(expected6));
            Assert.IsTrue(results.Contains(expected7));
            Assert.IsTrue(results.Contains(expected8));
        }

        [TestMethod, TestCategory("Build File Locations")]
        public void GetTsconfigBuildFilesForSolution()
        {
            List<string> results = BuildFileLocations.GetBuildFilesToLint(isBuildingSolution: true,
                                                                          selectedItems: null, useTsConfig: true).ToList();

            // Should be the same as if we are linting the solution (see TsconfigLocationsTest.FindInSelectedItemsSolution)
            Assert.AreEqual(3, results.Count);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");


            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));

        }

        [TestMethod, TestCategory("Build File Locations")]
        public void GetTsconfigBuildFilesForProject()
        {
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = TsconfigLocationsTest.FindProject(projectFullName, solution);
            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = project };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            List<string> results = BuildFileLocations.GetBuildFilesToLint(isBuildingSolution: false,
                                                                          selectedItems: selectedItems, useTsConfig: true).ToList();

            Assert.AreEqual(3, results.Count);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");

            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
        }

        [TestMethod, TestCategory("Build File Locations")]
        public void GetTsconfigBuildFilesForSingleItem()
        {
            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
            Project project = TsconfigLocationsTest.FindProject(projectFullName, solution);
            ProjectItem projectItem = TsconfigLocationsTest.FindProjectItemInProject(projectItemFullName, project);

            MockUIHierarchyItem mockSolutionHierarchyItem = new MockUIHierarchyItem() { Object = projectItem };
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[] { mockSolutionHierarchyItem };

            List<string> results = BuildFileLocations.GetBuildFilesToLint(isBuildingSolution: false,
                                                                          selectedItems: selectedItems, useTsConfig: true).ToList();

            // Again we are going to build the project the item is in so should lint for all tsconfigs in the project
            Assert.AreEqual(3, results.Count);

            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");

            Assert.IsTrue(results.Contains(expected1));
            Assert.IsTrue(results.Contains(expected2));
            Assert.IsTrue(results.Contains(expected3));
        }
    }
}
