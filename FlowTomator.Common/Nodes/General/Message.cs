using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common.Nodes
{
    [Node("Message", "General", "Shows a message box with parameterable content")]
    public class Message : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                return new Variable[] { text, title, buttons, icon };
            }
        }

        private Variable<string> text = new Variable<string>("Text", "Text", "The text to display in this message box");
        private Variable<string> title = new Variable<string>("Title", "Title", "The title of this message box");

        private Variable<MessageBoxButtons> buttons = new Variable<MessageBoxButtons>("Buttons", MessageBoxButtons.OKCancel, "The buttons to display in this message box");
        private Variable<MessageBoxIcon> icon = new Variable<MessageBoxIcon>("Icon", MessageBoxIcon.None, "The icon to display in this message box");

        public override NodeResult Run()
        {
            DialogResult result = MessageBox.Show(text.Value, title.Value, buttons.Value, icon.Value);

            switch (result)
            {
                case DialogResult.OK:
                case DialogResult.Yes:
                case DialogResult.No:
                    return NodeResult.Success;

                case DialogResult.Cancel:
                    return NodeResult.Stop;

                case DialogResult.Ignore:
                case DialogResult.Abort:
                    return NodeResult.Skip;
            }

            return NodeResult.Fail;
        }
    }
}