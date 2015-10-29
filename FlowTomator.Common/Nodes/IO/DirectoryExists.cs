using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("DirectoryExists", "IO", "Checks if the specified directory exists")]
    public class DirectoryExists : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return directory;
            }
        }

        private Variable<DirectoryInfo> directory = new Variable<DirectoryInfo>("Directory", null, "The directory to check");

        public override NodeStep Evaluate()
        {
            if (directory.Value == null)
                return new NodeStep(NodeResult.Fail);

            directory.Value.Refresh();

            return new NodeStep(NodeResult.Success, directory.Value?.Exists == true ? TrueSlot : FalseSlot);
        }
    }
}