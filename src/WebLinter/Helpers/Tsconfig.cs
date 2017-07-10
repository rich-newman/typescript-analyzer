using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WebLinter.Helpers
{
    public class Tsconfig
    {
        private FileInfo fileInfo;
        public static Tsconfig[] FindInProject(string projectFullName)
        {
            
            return null;
        }

        public static Tsconfig FindFromProjectItem(string projectItemFullName)
        {
            Tsconfig tsconfig = new Tsconfig("");

            return null;
        }

        public Tsconfig(string fileName)
        {
            fileInfo = new FileInfo(fileName);
            //fileInfo.n
        }

        public string FullName { get { return fileInfo.FullName; } }
    }
}
