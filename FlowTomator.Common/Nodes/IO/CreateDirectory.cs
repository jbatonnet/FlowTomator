using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("CreateDirectory", "IO", "Create a directory at the specified location")]
    public class CreateDirectory : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return directory;
            }
        }

        private Variable<DirectoryInfo> directory = new Variable<DirectoryInfo>("Directory", null, "The directory to be created");

        public override NodeResult Run()
        {
            if (directory.Value == null || directory.Value.Exists)
                return NodeResult.Skip;

            try
            {
                Directory.CreateDirectory(directory.Value.FullName);
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}
