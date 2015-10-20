using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("ReadFile", "IO", "Reads the content of the specified file")]
    public class ReadFile : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return file;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return content;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file to be read");
        private Variable content = new Variable("Content", typeof(object), null, "The content of the file to read");

        public override NodeResult Run()
        {
            if (file.Value == null)
                return NodeResult.Skip;

            try
            {
                content.Value = File.ReadAllText(file.Value.FullName);
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}