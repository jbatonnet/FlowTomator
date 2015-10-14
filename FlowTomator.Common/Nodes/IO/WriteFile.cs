using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("WriteFile", "IO", "Writes the specified content in the specified file")]
    public class WriteFile : Task
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
        private Variable content = new Variable("Content", typeof(object), null, "The content of the file to write");

        public override NodeResult Run()
        {
            if (file.Value == null || content.Value == null)
                return NodeResult.Skip;

            try
            {
                if (content.Value.GetType() == typeof(Bitmap) || content.Value.GetType().IsSubclassOf(typeof(Bitmap)))
                    (content.Value as Bitmap).Save(file.Value.FullName);
                else
                {
                    string text = content.Value.ToString();
                    File.WriteAllText(file.Value.FullName, text);
                }
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}