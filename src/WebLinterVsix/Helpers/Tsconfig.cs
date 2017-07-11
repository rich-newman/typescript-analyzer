using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace WebLinterVsix.Helpers
{
    /// <summary>
    /// Represents a tsconfig.json file
    /// A simple wrapper around FileInfo currently: parsing to be added
    /// </summary>
    public class Tsconfig
    {
        private FileInfo fileInfo;

        public Tsconfig(string fileName)
        {
            fileInfo = new FileInfo(fileName);
        }

        public string FullName { get { return fileInfo.FullName; } }
    }
}
