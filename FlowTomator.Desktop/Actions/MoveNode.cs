using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlowTomator.Desktop
{
    public class MoveNodeAction : Action
    {
        public NodeInfo NodeInfo { get; private set; }

        public Point OldPosition { get; private set; }
        public Point NewPosition { get; private set; }

        public MoveNodeAction(NodeInfo nodeInfo, Point position)
        {
            NodeInfo = nodeInfo;

            OldPosition = new Point(NodeInfo.X, NodeInfo.Y);
            NewPosition = position;
        }
        public MoveNodeAction(NodeInfo nodeInfo, Point oldPosition, Point newPosition)
        {
            NodeInfo = nodeInfo;

            OldPosition = oldPosition;
            NewPosition = newPosition;
        }

        public override void Do()
        {
            NodeInfo.X = NewPosition.X;
            NodeInfo.Y = NewPosition.Y;
        }

        public override void Undo()
        {
            NodeInfo.X = OldPosition.X;
            NodeInfo.Y = OldPosition.Y;
        }
    }
}