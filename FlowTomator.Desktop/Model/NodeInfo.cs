using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public enum NodeStatus
    {
        Idle,
        Running,
        Paused
    }

    public class NodeInfo : DependencyModel
    {
        private static Dictionary<Node, NodeInfo> nodeInfos = new Dictionary<Node, NodeInfo>();

        public Node Node { get; private set; }
        public FlowInfo FlowInfo { get; private set; }
        public AnchorBinder NodeAnchorBinder { get; } = new AnchorBinder();

        public NodeStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                NotifyPropertyChanged();
            }
        }
        public NodeResult? Result
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
                NotifyPropertyChanged();
            }
        }
        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                NotifyPropertyChanged();
            }
        }

        [DependsOn(nameof(Status), nameof(Result), nameof(Selected))]
        public Color StatusColor
        {
            get
            {
                if (Selected)
                    return Colors.DeepSkyBlue;

                switch (Status)
                {
                    case NodeStatus.Running: return Colors.LimeGreen;
                    case NodeStatus.Paused: return Colors.Gold;
                }

                switch (result)
                {
                    case NodeResult.Fail: return Colors.Red;
                }

                return Colors.Transparent;
            }
        }

        public Type Type
        {
            get
            {
                return Node.GetType();
            }
        }
        public NodeTypeInfo TypeInfo
        {
            get
            {
                return NodeTypeInfo.From(Type);
            }
        }
        public NodeAttribute Attribute
        {
            get
            {
                return Type.GetCustomAttributes(typeof(NodeAttribute), false).OfType<NodeAttribute>().FirstOrDefault();
            }
        }

        public IEnumerable<VariableInfo> Inputs
        {
            get
            {
                return Node.Inputs.Select(i => VariableInfo.From(this, i));
            }
        }
        public IEnumerable<VariableInfo> Outputs
        {
            get
            {
                return Node.Outputs.Select(o => VariableInfo.From(this, o));
            }
        }
        public IEnumerable<SlotInfo> Slots
        {
            get
            {
                return Node.Slots.Select(s => SlotInfo.From(this, s));
            }
        }

        public double X
        {
            get
            {
                object value;
                Node.Metadata.TryGetValue("Position.X", out value);

                if (value is int) return (int)value;
                else if (value is float) return (float)value;
                else if (value is double) return (double)value;

                return 0;
            }
            set
            {
                if (Node.Metadata.ContainsKey("Position.X"))
                    Node.Metadata["Position.X"] = value;
                else
                    Node.Metadata.Add("Position.X", value);

                NotifyPropertyChanged();
            }
        }
        public double Y
        {
            get
            {
                object value;
                Node.Metadata.TryGetValue("Position.Y", out value);

                if (value is int) return (int)value;
                else if (value is float) return (float)value;
                else if (value is double) return (double)value;

                return 0;
            }
            set
            {
                if (Node.Metadata.ContainsKey("Position.Y"))
                    Node.Metadata["Position.Y"] = value;
                else
                    Node.Metadata.Add("Position.Y", value);

                NotifyPropertyChanged();
            }
        }

        private NodeStatus status;
        private NodeResult? result;
        private bool selected;
        private double width, height;

        private NodeInfo(FlowInfo flowInfo, Node node)
        {
            FlowInfo = flowInfo;
            Node = node;
            NodeAnchorBinder.PropertyChanged += NodeAnchorBinder_PropertyChanged;
        }

        public void Update()
        {
            NotifyPropertyChanged(nameof(Inputs));
            NotifyPropertyChanged(nameof(Outputs));
            NotifyPropertyChanged(nameof(Slots));
        }

        private void NodeAnchorBinder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(NodeAnchorBinder));
        }

        public static NodeInfo From(FlowInfo flowInfo, Node node)
        {
            NodeInfo nodeInfo;

            if (!nodeInfos.TryGetValue(node, out nodeInfo))
                nodeInfos.Add(node, nodeInfo = new NodeInfo(flowInfo, node));

            return nodeInfo;
        }
    }
}