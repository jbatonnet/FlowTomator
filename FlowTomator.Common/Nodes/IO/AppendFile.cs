using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("AppendFile", "IO", "Appends the specified content in the specified file")]
    public class AppendFile : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return file;
                yield return content;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file to be written");
        private Variable content = new Variable("Content", typeof(object), null, "The content of the file to append");

        public override NodeResult Run()
        {
            if (file.Value == null || content.Value == null)
                return NodeResult.Skip;

            try
            {
                string text = content.Value.ToString();
                File.AppendAllText(file.Value.FullName, text);
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}