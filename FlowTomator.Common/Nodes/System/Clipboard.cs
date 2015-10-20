using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common
{
    [Node("SetClipboard", "System", "Replace clipboard content with the specified content")]
    public class SetClipboard : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return content;
            }
        }

        private Variable content = new Variable("Content", typeof(object), null, "The content of the clipboard to replace");

        public override NodeResult Run()
        {
            Thread thread = new Thread(() =>
            {
                Clipboard.SetDataObject(content.Value, true);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return NodeResult.Success;
        }
    }

    public enum ClipboardDataType
    {
        String,
        Image
    }

    [Node("GetClipboard", "System", "Get the current clipboard content")]
    public class GetClipboard : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return dataType;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return content;
            }
        }

        private Variable<ClipboardDataType> dataType = new Variable<ClipboardDataType>("DataType", ClipboardDataType.String, "The type of the content to get");
        private Variable content = new Variable("Content", typeof(object), null, "The content of the clipboard");

        public override NodeResult Run()
        {
            if (dataType.Value == ClipboardDataType.String && Clipboard.ContainsText())
            {
                content.Value = Clipboard.GetText();
                return NodeResult.Success;
            }
            else if (dataType.Value == ClipboardDataType.Image && Clipboard.ContainsImage())
            {
                content.Value = Clipboard.GetImage();
                return NodeResult.Success;
            }
            else
                return NodeResult.Fail;
        }
    }
}