using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class AddLinkAction : Action
    {
        public SlotInfo SlotInfo { get; private set; }
        public NodeInfo NodeInfo { get; private set; }

        public AddLinkAction(SlotInfo slotInfo, NodeInfo nodeInfo)
        {
            SlotInfo = slotInfo;
            NodeInfo = nodeInfo;
        }

        public override void Do()
        {
            SlotInfo.Slot.Nodes.Add(NodeInfo.Node);
            NodeInfo.FlowInfo.Update();
        }

        public override void Undo()
        {
            SlotInfo.Slot.Nodes.Remove(NodeInfo.Node);
            NodeInfo.FlowInfo.Update();
        }
    }
}