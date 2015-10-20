using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("DeleteFile", "IO", "Delete the specified file")]
    public class DeleteFile : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return file;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file to be deleted");

        public override NodeResult Run()
        {
            if (file.Value == null || !file.Value.Exists)
                return NodeResult.Skip;

            try
            {
                File.Delete(file.Value.FullName);
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}