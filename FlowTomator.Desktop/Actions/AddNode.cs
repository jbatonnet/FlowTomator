using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class AddNodeAction : Action
    {
        public FlowInfo FlowInfo { get; private set; }
        public Node Node { get; private set; }

        public AddNodeAction(FlowInfo flow, Node node)
        {
            FlowInfo = flow;
            Node = node;
        }

        public override void Do()
        {
            FlowInfo.Flow.Nodes.Add(Node);
            FlowInfo.Update();
        }

        public override void Undo()
        {
            FlowInfo.Flow.Nodes.Remove(Node);
            FlowInfo.Update();
        }
    }
}