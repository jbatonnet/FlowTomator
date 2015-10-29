using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowTomator.Common
{
    [Node("PickFile", "IO", "Asks the user to pick a file")]
    public class PickFile : Node
    {
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return file;
            }
        }
        public override IEnumerable<Slot> Slots
        {
            get
            {
                yield return okSlot;
                yield return cancelSlot;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file picked by the user");

        private Slot okSlot = new Slot("OK");
        private Slot cancelSlot = new Slot("Cancel");

        public override NodeStep Evaluate()
        {
            DialogResult result = DialogResult.Cancel;
            string fileName = "";

            ManualResetEvent mre = new ManualResetEvent(false);

            Thread thread = new Thread(() =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.FileName = file.Value?.FullName;

                result = openFileDialog.ShowDialog();
                fileName = openFileDialog.FileName;

                mre.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            mre.WaitOne();
            if (result != DialogResult.OK)
                return new NodeStep(NodeResult.Success, cancelSlot);

            file.Value = new FileInfo(fileName);
            return new NodeStep(NodeResult.Success, okSlot);
        }
    }
}