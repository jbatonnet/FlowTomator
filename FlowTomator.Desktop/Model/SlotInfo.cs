using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class SlotInfo : INotifyPropertyChanged
    {
        private static Dictionary<Slot, SlotInfo> slotInfos = new Dictionary<Slot, SlotInfo>();

        public Slot Slot { get; private set; }
        public NodeInfo NodeInfo { get; private set; }
        public AnchorBinder SlotAnchorBinder { get; } = new AnchorBinder();

        public event PropertyChangedEventHandler PropertyChanged;

        public SlotInfo Info
        {
            get
            {
                return this;
            }
        }
        public IEnumerable<LinkInfo> Links
        {
            get
            {
                return Slot.Nodes.Select(n => LinkInfo.From(NodeInfo, Slot, n));
            }
        }

        private SlotInfo(NodeInfo nodeInfo, Slot slot)
        {
            Slot = slot;
            SlotAnchorBinder.PropertyChanged += SlotAnchorBinder_PropertyChanged;

            NodeInfo = nodeInfo;
        }

        private void SlotAnchorBinder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(SlotAnchorBinder)));
        }

        public static SlotInfo From(NodeInfo nodeInfo, Slot slot)
        {
            SlotInfo slotInfo;

            if (!slotInfos.TryGetValue(slot, out slotInfo))
                slotInfos.Add(slot, slotInfo = new SlotInfo(nodeInfo, slot));

            return slotInfo;
        }
    }
}