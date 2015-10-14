using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class LinkInfo : INotifyPropertyChanged
    {
        private static Dictionary<Tuple<Slot, Node>, LinkInfo> linkInfos = new Dictionary<Tuple<Slot, Node>, LinkInfo>();

        public event PropertyChangedEventHandler PropertyChanged;

        public Slot Slot { get; private set; }
        public SlotInfo SlotInfo { get; private set; }
        public AnchorBinder SlotAnchorBinder
        {
            get
            {
                return SlotInfo.SlotAnchorBinder;
            }
        }

        public Node Node { get; private set; }
        public NodeInfo NodeInfo { get; private set; }
        public AnchorBinder NodeAnchorBinder
        {
            get
            {
                return NodeInfo.NodeAnchorBinder;
            }
        }

        private LinkInfo(NodeInfo nodeInfo, Slot slot, Node node)
        {
            Slot = slot;
            SlotInfo = SlotInfo.From(nodeInfo, slot);
            SlotInfo.PropertyChanged += SlotInfo_PropertyChanged;

            Node = node;
            NodeInfo = NodeInfo.From(nodeInfo.FlowInfo, node);
            NodeInfo.PropertyChanged += NodeInfo_PropertyChanged;
        }

        private void SlotInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SlotAnchorBinder)));
        }
        private void NodeInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(NodeAnchorBinder)));
        }

        public static LinkInfo From(NodeInfo nodeInfo, Slot slot, Node node)
        {
            Tuple<Slot, Node> tuple = new Tuple<Slot, Node>(slot, node);
            LinkInfo linkInfo;

            if (!linkInfos.TryGetValue(tuple, out linkInfo))
                linkInfos.Add(tuple, linkInfo = new LinkInfo(nodeInfo, slot, node));

            return linkInfo;
        }
    }
}