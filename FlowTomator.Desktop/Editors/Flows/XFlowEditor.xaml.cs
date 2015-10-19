using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    [FlowEditor(typeof(XFlow))]
    public partial class XFlowEditor : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<NodeInfo> SelectedNodes { get; } = new ObservableCollection<NodeInfo>();

        public AnchorBinder SourceAnchorBinder { get; } = new AnchorBinder();
        public AnchorBinder DestinationAnchorBinder { get; } = new AnchorBinder();
        public Visibility NewLinkVisibility
        {
            get
            {
                return creatingNewLink ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public FlowInfo FlowInfo
        {
            get
            {
                return DataContext as FlowInfo;
            }
        }
        public XFlow Flow
        {
            get
            {
                return FlowInfo.Flow as XFlow;
            }
        }

        private List<NodeControl> movingNodes = new List<NodeControl>();
        private Dictionary<NodeControl, Point?> movingNodeOrigins = new Dictionary<NodeControl, Point?>();
        private Dictionary<NodeControl, Point?> movingNodeOffsets = new Dictionary<NodeControl, Point?>();
        private int currentZIndex = 1;

        private bool creatingNewLink = false;
        private SlotInfo newLinkSlot;

        public XFlowEditor()
        {
            Tag = new DependencyManager(this, (s, e) => PropertyChanged(s, e));
            InitializeComponent();

            DataContextChanged += XFlowEditor_DataContextChanged;
            SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
        }

        private void XFlowEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FlowInfo.PropertyChanged += FlowInfo_PropertyChanged;
        }
        private void FlowInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //EditorThumbnail.GetBindingExpression(Image.SourceProperty).UpdateSource();
        }
        private void SelectedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedNodes.Count == 0)
            {
                foreach (NodeInfo nodeInfo in FlowInfo.Nodes)
                    nodeInfo.Opaque = true;
            }
            else
            {
                foreach (NodeInfo nodeInfo in FlowInfo.Nodes)
                    nodeInfo.Opaque = false;

                foreach (NodeInfo nodeInfo in SelectedNodes)
                    nodeInfo.Opaque = true;
            }

            movingNodes = SelectedNodes.Select(n => VisualTreeHelper.GetChild(NodeList.ItemContainerGenerator.ContainerFromItem(n), 0) as NodeControl).ToList();
            movingNodeOrigins = movingNodes.ToDictionary(n => n, n => movingNodeOrigins.ContainsKey(n) ? movingNodeOrigins[n] : null);
            movingNodeOffsets = movingNodes.ToDictionary(n => n, n => movingNodeOffsets.ContainsKey(n) ? movingNodeOffsets[n] : null);
        }

        private void NodeControl_HeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            NodeControl nodeControl = sender as NodeControl;
            ContentPresenter contentPresenter = VisualTreeHelper.GetParent(nodeControl) as ContentPresenter;
            Canvas canvas = VisualTreeHelper.GetParent(contentPresenter) as Canvas;

            contentPresenter.SetValue(Panel.ZIndexProperty, currentZIndex++);

            if (!SelectedNodes.Contains(nodeControl.NodeInfo))
            {
                if (Keyboard.IsKeyUp(Key.LeftCtrl) && Keyboard.IsKeyUp(Key.RightCtrl) && Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift))
                    SelectedNodes.Clear();

                SelectedNodes.Add(nodeControl.NodeInfo);
            }

            foreach (NodeControl n in movingNodes)
            {
                movingNodeOrigins[n] = new Point(n.NodeInfo.X, n.NodeInfo.Y);
                movingNodeOffsets[n] = e.GetPosition(n);
            }
        }
        private void NodeControl_SlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            SlotInfo slotInfo = sender as SlotInfo;

            Border border = e.OriginalSource as Border;
            Anchor anchor = VisualTreeHelper.GetChild(border, 0) as Anchor;

            newLinkSlot = slotInfo;

            SourceAnchorBinder.Anchor = anchor;
            DestinationAnchorBinder.Anchor = anchor;

            creatingNewLink = true;

            NotifyPropertyChanged(nameof(SourceAnchorBinder));
            NotifyPropertyChanged(nameof(DestinationAnchorBinder));
            NotifyPropertyChanged(nameof(NewLinkVisibility));
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource != sender)
                return;

            SelectedNodes.Clear();

            foreach (NodeInfo nodeInfo in FlowInfo.Nodes)
                foreach (VariableInfo variable in nodeInfo.Inputs)
                    variable.Selected = false;
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            Canvas canvas = sender as Canvas;

            if (movingNodes.Count > 0)
            {
                Point mousePosition = e.GetPosition(canvas);

                foreach (NodeControl nodeControl in movingNodes)
                {
                    ContentPresenter contentPresenter = VisualTreeHelper.GetParent(nodeControl) as ContentPresenter;
                    Point nodeControlPosition = new Point(mousePosition.X - movingNodeOffsets[nodeControl].Value.X, mousePosition.Y - movingNodeOffsets[nodeControl].Value.Y);

                    nodeControlPosition.X = Math.Round(nodeControlPosition.X / 4) * 4;
                    nodeControlPosition.Y = Math.Round(nodeControlPosition.Y / 4) * 4;

                    contentPresenter.SetValue(Canvas.LeftProperty, nodeControlPosition.X);
                    contentPresenter.SetValue(Canvas.TopProperty, nodeControlPosition.Y);
                }
            }

            if (creatingNewLink)
            {
                Border border = e.OriginalSource as Border;
                Anchor anchor = border == null ? null : VisualTreeHelper.GetChild(border, 0) as Anchor;

                if (anchor != null)
                {
                    DestinationAnchorBinder.Anchor = anchor;
                }
                else
                {
                    Point mousePosition = e.GetPosition(canvas);

                    DestinationAnchorBinder.Anchor = DestinationAnchor;
                    DestinationAnchorBinder.Anchor.SetValue(Canvas.LeftProperty, mousePosition.X);
                    DestinationAnchorBinder.Anchor.SetValue(Canvas.TopProperty, mousePosition.Y);
                }

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(DestinationAnchorBinder)));
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (movingNodes.Count > 0)
            {
                foreach (NodeControl nodeControl in movingNodes)
                {
                    Point movingNodeOrigin = movingNodeOrigins[nodeControl].Value;

                    if (movingNodeOrigin.X != nodeControl.NodeInfo.X || movingNodeOrigin.Y != nodeControl.NodeInfo.Y)
                        FlowInfo.History.Do(new MoveNodeAction(nodeControl.NodeInfo, movingNodeOrigin, new Point(nodeControl.NodeInfo.X, nodeControl.NodeInfo.Y)));
                }
            }

            if (creatingNewLink)
            {
                Border border = e.OriginalSource as Border;

                if (border != null)
                {
                    StackPanel stackPanel = VisualTreeHelper.GetParent(border) as StackPanel;

                    if (stackPanel != null)
                    {
                        DependencyObject nodeControl = border;
                        while (!(nodeControl is NodeControl))
                            nodeControl = VisualTreeHelper.GetParent(nodeControl);

                        FlowInfo.History.Do(new AddLinkAction(newLinkSlot, (nodeControl as NodeControl).NodeInfo));
                    }
                }

                creatingNewLink = false;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(NewLinkVisibility)));
            }
        }
        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FlowTomator.Node") || sender == e.Source)
                e.Effects = DragDropEffects.None;
        }
        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            Type nodeType = e.Data.GetData("FlowTomator.Node") as Type;

            Point mousePosition = e.GetPosition(canvas);

            // Create new node
            Node node = Activator.CreateInstance(nodeType) as Node;
            node.Metadata.Add("Position.X", mousePosition.X);
            node.Metadata.Add("Position.Y", mousePosition.Y);

            FlowInfo.History.Do(new AddNodeAction(FlowInfo, node));
        }

        private void RemoveLinkItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            LinkInfo linkInfo = menuItem.Tag as LinkInfo;

            FlowInfo.History.Do(new DeleteLinkAction(linkInfo));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}