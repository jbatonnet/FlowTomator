using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("FileExists", "IO", "Checks if the specified file exists")]
    public class FileExists : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return file;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file to be created");

        public override NodeStep Evaluate()
        {
            if (file.Value == null)
                return new NodeStep(NodeResult.Fail);

            return new NodeStep(NodeResult.Success, file.Value?.Exists == true ? TrueSlot : FalseSlot);
        }
    }
}