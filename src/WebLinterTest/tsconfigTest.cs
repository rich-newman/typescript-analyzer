//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.IO;
//using System.Linq;
//using WebLinter;
//using WebLinterVsix.Helpers;
//using System.Collections.Generic;
//using EnvDTE;

//namespace WebLinterTest
//{
//    [TestClass]
//    public class TsconfigTest
//    {
//        private static EnvDTE80.DTE2 dte = null;
//        private static EnvDTE.Solution solution = null;

//        // Clunky, but better than nothing
//        [ClassInitialize]
//        public static void TestInitialize(TestContext testContext)
//        {
//            Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.15.0");
//            object inst = System.Activator.CreateInstance(type, true);
//            dte = (EnvDTE80.DTE2)inst;
//            dte.Solution.Open(Path.GetFullPath(@"../../artifacts/tsconfig/Tsconfig.sln"));
//            solution = dte.Solution;

//            WebLinterVsix.WebLinterPackage.Settings = new WebLinterTest.Settings();
//        }

//        [ClassCleanup]
//        public static void TestCleanup()
//        {
//            if (solution != null) { solution.Close(); solution = null; }
//            if (dte != null) dte.Quit();
//            WebLinterVsix.WebLinterPackage.Settings = null;
//        }

//        [TestMethod]
//        public void Basic()
//        {

//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindForSingleItem()
//        {
//            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/file1.ts");
//            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
//            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
//            Assert.AreEqual(expected, result.FullName);
//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindForSingleItemSubfolder()
//        {
//            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/test.ts");
//            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
//            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
//            Assert.AreEqual(expected, result.FullName);
//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindForSingleItemRoot()
//        {
//            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/c/file4.ts");
//            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
//            string expected = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
//            Assert.AreEqual(expected, result.FullName);
//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindForSingleItemNotsconfig()
//        {
//            // Note there's a tsconfig.json in the folder, but it's not in the project: it shouldn't be picked up
//            string projectItemFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/b/file2.ts");
//            Tsconfig result = TsconfigLocations.FindFromProjectItem(projectItemFullName, solution);
//            Assert.IsNull(result);
//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindInProject()
//        {
//            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfigTest.csproj");
//            Project project = FindProject(projectFullName, solution);
//            Tsconfig[] results = TsconfigLocations.FindInProject(project, solution).ToArray();
//            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
//            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
//            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
//            Assert.AreEqual(3, results.Length);
//            Assert.IsTrue(Contains(results, expected1));
//            Assert.IsTrue(Contains(results, expected2));
//            Assert.IsTrue(Contains(results, expected3));
//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindInProjectNotsconfig()
//        {
//            string projectFullName = Path.GetFullPath(@"../../artifacts/tsconfig/none/tsconfigEmptyTest.csproj");
//            Project project = FindProject(projectFullName, solution);
//            Tsconfig[] results = TsconfigLocations.FindInProject(project, solution).ToArray();
//            Assert.IsNotNull(results);
//            Assert.AreEqual(0, results.Length);
//        }

//        [TestMethod, TestCategory("tsconfig")]
//        public void FindInSolution()
//        {
//            Tsconfig[] results = TsconfigLocations.FindInSolution(solution).ToArray();
//            string expected1 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/tsconfig.json");
//            string expected2 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/a/tsconfig.json");
//            string expected3 = Path.GetFullPath(@"../../artifacts/tsconfig/multiple/b/tsconfig.json");
//            Assert.AreEqual(3, results.Length);
//            Assert.IsTrue(Contains(results, expected1));
//            Assert.IsTrue(Contains(results, expected2));
//            Assert.IsTrue(Contains(results, expected3));
//        }

//        private static Project FindProject(string projectFullName, Solution solution)
//        {
//            var test = solution.Projects.GetEnumerator();
//            foreach (Project project in solution.Projects)
//                if (project.FullName == projectFullName) return project;
//            return null;
//        }

//        public bool Contains(Tsconfig[] ary, string value)
//        {
//            foreach (Tsconfig item in ary)
//            {
//                if (item.FullName == value) return true;
//            }
//            return false;
//        }
//    }
//}
