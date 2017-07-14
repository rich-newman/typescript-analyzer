using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebLinter
{
    public class ServerPostData
    {
        [JsonProperty("config")]
        public string Config { get; set; }

        [JsonProperty("files")]
        public IEnumerable<string> Files { get; set; }

        [JsonProperty("fixerrors")]
        public bool FixErrors { get; set; }

        [JsonProperty("usetsconfig")]
        public bool UseTSConfig { get; set; }

        [JsonProperty("debug")]
        public bool Debug { get; set; }
    }
}
