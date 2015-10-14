using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    /*public class WriteFile : Task
    {
        public Variable Path { get; } = new Variable("Path", typeof(string), "The path of the file to be written");
        public Variable Content { get; } = new Variable("Content", typeof(string), "The content of the file to write");

        public override TaskResult Run()
        {
            if (!File.Exists(Path.ToString()))
                return TaskResult.Fail;

            File.WriteAllText(Path.ToString(), Content.ToString());
            return TaskResult.Success;
        }
    }*/
}