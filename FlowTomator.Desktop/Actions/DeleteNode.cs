using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class DeleteNodeAction : Action
    {
        public FlowInfo FlowInfo { get; private set; }
        public Node Node { get; private set; }

        private List<Slot> slots = new List<Slot>();

        public DeleteNodeAction(NodeInfo nodeInfo)
        {
            FlowInfo = nodeInfo.FlowInfo;
            Node = nodeInfo.Node;
        }

        public override void Do()
        {
            slots.Clear();

            // Remove the node
            FlowInfo.Flow.Nodes.Remove(Node);

            // Remove all links to this node
            foreach (Node node in FlowInfo.Flow.Nodes)
                foreach (Slot slot in node.Slots)
                    while (slot.Nodes.Contains(Node))
                    {
                        slot.Nodes.Remove(Node);
                        slots.Add(slot);
                    }

            FlowInfo.Update();
        }

        public override void Undo()
        {
            // Add the node
            FlowInfo.Flow.Nodes.Add(Node);

            // Restore all links to this node
            foreach (Slot slot in slots)
                slot.Nodes.Add(Node);

            FlowInfo.Update();
        }
    }
}