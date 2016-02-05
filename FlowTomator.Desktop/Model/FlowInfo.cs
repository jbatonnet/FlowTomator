using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class FlowInfo : DependencyModel
    {
        private static Dictionary<Flow, FlowInfo> flowInfos = new Dictionary<Flow, FlowInfo>();

        public EditableFlow Flow { get; private set; }
        public HistoryInfo History { get; } = new HistoryInfo();

        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                NotifyPropertyChanged();
            }
        }
        [DependsOn(nameof(Path))]
        public string Name
        {
            get
            {
                return Path == null ? "New flow" : System.IO.Path.GetFileName(Path);
            }
        }

        public Type Type
        {
            get
            {
                return Flow.GetType();
            }
        }
        public IEnumerable<NodeInfo> Nodes
        {
            get
            {
                return Flow.GetAllNodes()
                           .Select(n => NodeInfo.From(this, n));
            }
        }
        public IEnumerable<LinkInfo> Links
        {
            get
            {
                foreach (Node node in Flow.GetAllNodes())
                    foreach (Slot slot in node.Slots)
                        foreach (Node linkedNode in slot.Nodes)
                            yield return LinkInfo.From(NodeInfo.From(this, node), slot, linkedNode);
            }
        }
        public ICollection<VariableInfo> Variables
        {
            get
            {
                return Flow.Variables.Select(v => VariableInfo.From(this, v)).ToList();
            }
        }

        private string path;

        public FlowInfo(EditableFlow flow, string path)
        {
            Flow = flow;
            this.path = path;

            /*foreach (NodeInfo node in Nodes)
            {
                foreach (VariableInfo input in node.Inputs)
                    if (input.Variable.Linked != null && !Variables.Any(v => v.Variable == input.Variable.Linked))
                        Variables.Add(VariableInfo.From(this, input.Variable.Linked));

                foreach (VariableInfo output in node.Outputs)
                    if (output.Variable.Linked != null && !Variables.Any(v => v.Variable == output.Variable.Linked))
                        Variables.Add(VariableInfo.From(this, output.Variable.Linked));
            }*/
        }

        public void Update()
        {
            NotifyPropertyChanged(nameof(Nodes), nameof(Links));
        }
    }
}