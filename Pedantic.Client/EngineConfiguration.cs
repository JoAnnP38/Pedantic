using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Client
{
    public class EngineConfiguration
    {
        public string name { get; set; } = string.Empty;
        public string command { get; set; } = string.Empty;
        public string protocol { get; set; } = string.Empty;
        public string workingDirectory { get; set; } = string.Empty;
        public string stderrFile { get; set; } = string.Empty;
        public string[] initStrings { get; set; } = Array.Empty<string>();
        public bool whitepov { get; set; } = false;
        public bool ponder { get; set; } = false;
    }
}
