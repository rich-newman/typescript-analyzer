using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebLinter
{
    public class ServerPostData
    {
        public string Config { get; set; }
        public IEnumerable<string> Files { get; set; }
        public bool FixErrors { get; set; }
    }
}
