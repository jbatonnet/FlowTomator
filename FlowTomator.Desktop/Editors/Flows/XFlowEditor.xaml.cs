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

        public bool Selecting
        {
            get
            {
                return selecting;
            }
            set
            {
                selecting = value;
                NotifyPropertyChanged();
            }
        }
        public Point SelectionStart
        {
            get
            {
                return selectionStart;
            }
            set
            {
                selectionStart = value;
                NotifyPropertyChanged();
            }
        }
        public Point SelectionEnd
        {
            get
            {
                return selectionEnd;
            }
            set
            {
                selectionEnd = value;
                NotifyPropertyChanged();
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionX
        {
            get
            {
                return Math.Min(selectionStart.X, selectionEnd.X);
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionY
        {
            get
            {
                return Math.Min(selectionStart.Y, selectionEnd.Y);
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionWidth
        {
            get
            {
                return Math.Max(selectionStart.X, selectionEnd.X) - SelectionX;
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionHeight
        {
            get
            {
                return Math.Max(selectionStart.Y, selectionEnd.Y) - SelectionY;
            }
        }
        [DependsOn(nameof(Selecting))]
        public Visibility SelectionVisibility
        {
            get
            {
                return selecting ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Dictionary<NodeInfo, NodeControl> nodeControls = new Dictionary<NodeInfo, NodeControl>();

        private Dictionary<NodeControl, Point?> movingNodeOrigins = new Dictionary<NodeControl, Point?>();
        private Dictionary<NodeControl, Point?> movingNodeOffsets = new Dictionary<NodeControl, Point?>();
        private int currentZIndex = 1;

        private bool creatingNewLink = false;
        private SlotInfo newLinkSlot;

        public bool selecting;
        public Point selectionStart, selectionEnd;

        public XFlowEditor()
        {
            Tag = new DependencyManager(this, (s, e) => PropertyChanged(s, e));
            InitializeComponent();

            DataContextChanged += XFlowEditor_DataContextChanged;
            Loaded += XFlowEditor_Loaded;

            SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
        }

        private void XFlowEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FlowInfo.PropertyChanged += FlowInfo_PropertyChanged;
        }
        private void XFlowEditor_Loaded(object sender, RoutedEventArgs e)
        {
            nodeControls = FlowInfo.Nodes.ToDictionary(n => n, n => VisualTreeHelper.GetChild(NodeList.ItemContainerGenerator.ContainerFromItem(n), 0) as NodeControl);
        }
        private void FlowInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //EditorThumbnail.GetBindingExpression(Image.SourceProperty).UpdateSource();
        }
        private void SelectedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (NodeInfo nodeInfo in FlowInfo.Nodes)
                nodeInfo.Selected = false;

            foreach (NodeInfo nodeInfo in SelectedNodes)
                nodeInfo.Selected = true;
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

            movingNodeOrigins = SelectedNodes.Select(n => nodeControls[n]).ToDictionary(n => n, n => movingNodeOrigins.ContainsKey(n) ? movingNodeOrigins[n] : null);
            movingNodeOffsets = SelectedNodes.Select(n => nodeControls[n]).ToDictionary(n => n, n => movingNodeOffsets.ContainsKey(n) ? movingNodeOffsets[n] : null);

            foreach (NodeControl n in movingNodeOrigins.Keys.ToArray())
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

            SelectionStart = SelectionEnd = e.GetPosition(sender as Canvas);
            Selecting = true;
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            Canvas canvas = sender as Canvas;

            if (movingNodeOrigins.Count > 0)
            {
                Point mousePosition = e.GetPosition(canvas);

                foreach (NodeControl nodeControl in movingNodeOrigins.Keys)
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

            if (selecting)
            {
                SelectionEnd = e.GetPosition(sender as Canvas);
                SelectedNodes.Clear();

                Rect selectionRect = new Rect(SelectionStart, SelectionEnd);

                foreach (var pair in nodeControls)
                {
                    Rect nodeRect = new Rect(pair.Key.X, pair.Key.Y, pair.Value.RenderSize.Width, pair.Value.RenderSize.Height);

                    if (selectionRect.IntersectsWith(nodeRect))
                        SelectedNodes.Add(pair.Key);
                }
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (movingNodeOrigins.Count > 0)
            {
                foreach (NodeControl nodeControl in movingNodeOrigins.Keys)
                {
                    Point movingNodeOrigin = movingNodeOrigins[nodeControl].Value;

                    if (movingNodeOrigin.X != nodeControl.NodeInfo.X || movingNodeOrigin.Y != nodeControl.NodeInfo.Y)
                        FlowInfo.History.Do(new MoveNodeAction(nodeControl.NodeInfo, movingNodeOrigin, new Point(nodeControl.NodeInfo.X, nodeControl.NodeInfo.Y)));
                }

                movingNodeOrigins.Clear();
                movingNodeOffsets.Clear();
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

            Selecting = false;
            SelectedNodes_CollectionChanged(null, null);
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