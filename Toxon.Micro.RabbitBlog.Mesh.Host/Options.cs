using System.Collections.Generic;
using CommandLine;

namespace Toxon.Micro.RabbitBlog.Mesh.Host
{
    public class Options
    {
        [Option('b', "base", Required = false, Separator = ',', Default = new[] { "127.0.0.1:17999" }, HelpText = "Comma-separated list of known mesh base endpoints")]
        public IEnumerable<string> BaseAddresses { get; set; }

        [Value(0, MetaName = "assembly-name", HelpText = "The name of the assembly to be loaded by the host")]
        public string AssemblyPath { get; set; }
    }
}