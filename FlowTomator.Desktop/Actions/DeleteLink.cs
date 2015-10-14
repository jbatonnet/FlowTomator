using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class DeleteLinkAction : Action
    {
        public LinkInfo LinkInfo { get; private set; }

        public DeleteLinkAction(LinkInfo linkInfo)
        {
            LinkInfo = linkInfo;
        }

        public override void Do()
        {
            LinkInfo.Slot.Nodes.Remove(LinkInfo.Node);
            LinkInfo.NodeInfo.FlowInfo.Update();
        }

        public override void Undo()
        {
            LinkInfo.Slot.Nodes.Add(LinkInfo.Node);
            LinkInfo.NodeInfo.FlowInfo.Update();
        }
    }
}