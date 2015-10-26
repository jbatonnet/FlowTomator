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
                return Math.Min(selectionStart.X, selectionEnd.X) * Scale;
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionY
        {
            get
            {
                return Math.Min(selectionStart.Y, selectionEnd.Y) * Scale;
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionWidth
        {
            get
            {
                return Math.Max(selectionStart.X, selectionEnd.X) * Scale - SelectionX;
            }
        }
        [DependsOn(nameof(SelectionStart), nameof(SelectionEnd))]
        public double SelectionHeight
        {
            get
            {
                return Math.Max(selectionStart.Y, selectionEnd.Y) * Scale - SelectionY;
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

        public bool Panning
        {
            get
            {
                return panning;
            }
            set
            {
                panning = value;
                NotifyPropertyChanged();
            }
        }
        public double PanX
        {
            get
            {
                return panX;
            }
            set
            {
                panX = value;
                NotifyPropertyChanged();
            }
        }
        public double PanY
        {
            get
            {
                return panY;
            }
            set
            {
                panY = value;
                NotifyPropertyChanged();
            }
        }

        public double Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                NotifyPropertyChanged();
            }
        }
        
        [DependsOn(nameof(Panning))]
        public Cursor EditorCursor
        {
            get
            {
                return Panning ? Cursors.SizeAll : Cursors.Arrow;
            }
        }

        private Dictionary<NodeInfo, NodeControl> nodeControls = new Dictionary<NodeInfo, NodeControl>();

        private Dictionary<NodeControl, Point?> movingNodeOrigins = new Dictionary<NodeControl, Point?>();
        private Dictionary<NodeControl, Point?> movingNodeOffsets = new Dictionary<NodeControl, Point?>();
        private int currentZIndex = 1;

        private bool creatingNewLink = false;
        private SlotInfo newLinkSlot;

        private double scale = 1;

        private bool selecting;
        private Point selectionStart, selectionEnd;

        private bool panning;
        private Point panningStart;
        private double panX, panY;

        public XFlowEditor()
        {
            Tag = new DependencyManager(this, (s, e) => NotifyPropertyChanged(e.PropertyName));
            InitializeComponent();

            DataContextChanged += XFlowEditor_DataContextChanged;
            Loaded += XFlowEditor_Loaded;

            SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
        }

        private void XFlowEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FlowInfo oldFlow = e.OldValue as FlowInfo;
            if (oldFlow != null)
                oldFlow.PropertyChanged -= FlowInfo_PropertyChanged;

            FlowInfo newFlow = e.NewValue as FlowInfo;
            if (newFlow != null)
                newFlow.PropertyChanged += FlowInfo_PropertyChanged;
        }
        private void XFlowEditor_Loaded(object sender, RoutedEventArgs e)
        {
            nodeControls = FlowInfo.Nodes.ToDictionary(n => n, n => VisualTreeHelper.GetChild(NodeList.ItemContainerGenerator.ContainerFromItem(n), 0) as NodeControl);
        }
        private void FlowInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateLayout();

            nodeControls = FlowInfo.Nodes.ToDictionary(n => n, n =>
            {
                DependencyObject container = NodeList.ItemContainerGenerator.ContainerFromItem(n);

                if (container == null)
                    return null;
                if (VisualTreeHelper.GetChildrenCount(container) == 0)
                    return null;

                return VisualTreeHelper.GetChild(container, 0) as NodeControl;
            }).Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value);
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
            if (e.LeftButton == MouseButtonState.Released || Keyboard.IsKeyDown(Key.Space))
                return;

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
            if (e.LeftButton == MouseButtonState.Released || Keyboard.IsKeyDown(Key.Space))
                return;

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
        private void NodeControl_LayoutUpdated(object sender, EventArgs e)
        {

        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Space) || e.MiddleButton == MouseButtonState.Pressed)
            {
                Panning = true;
                panningStart = e.GetPosition(EditorScroller);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.OriginalSource != EditorScroller)
                    return;

                SelectedNodes.Clear();

                foreach (NodeInfo nodeInfo in FlowInfo.Nodes)
                    foreach (VariableInfo variable in nodeInfo.Inputs)
                        variable.Selected = false;

                Point mousePosition = e.GetPosition(EditorCanvas);

                Selecting = true;
                SelectionStart = SelectionEnd = new Point(mousePosition.X + PanX, mousePosition.Y + PanY);
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Canvas canvas = sender as Canvas;

            if (movingNodeOrigins.Count > 0)
            {
                Point mousePosition = e.GetPosition(canvas);
                mousePosition = new Point(mousePosition.X - PanX, mousePosition.Y - PanY);

                foreach (NodeControl nodeControl in movingNodeOrigins.Keys)
                {
                    ContentPresenter contentPresenter = VisualTreeHelper.GetParent(nodeControl) as ContentPresenter;
                    Point nodeControlPosition = new Point(mousePosition.X / Scale - movingNodeOffsets[nodeControl].Value.X, mousePosition.Y / Scale - movingNodeOffsets[nodeControl].Value.Y);

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
                    DestinationAnchorBinder.Anchor.SetValue(Canvas.LeftProperty, mousePosition.X / Scale - PanX);
                    DestinationAnchorBinder.Anchor.SetValue(Canvas.TopProperty, mousePosition.Y / Scale - PanY);
                }

                NotifyPropertyChanged(nameof(DestinationAnchorBinder));
            }

            if (Panning)
            {
                Point panningPosition = e.GetPosition(EditorScroller);

                PanX += (panningPosition.X - panningStart.X) / Scale;
                PanY += (panningPosition.Y - panningStart.Y) / Scale;

                panningStart = panningPosition;
            }

            if (Selecting)
            {
                Point mousePosition = e.GetPosition(EditorCanvas);

                SelectionEnd = new Point(mousePosition.X + PanX, mousePosition.Y + PanY);
                SelectedNodes.Clear();

                Rect selectionRect = new Rect(SelectionStart, SelectionEnd);

                foreach (var pair in nodeControls)
                {
                    Rect nodeRect = new Rect(pair.Key.X + PanX, pair.Key.Y + PanY, pair.Value.ActualWidth, pair.Value.ActualHeight);

                    if (selectionRect.IntersectsWith(nodeRect))
                        SelectedNodes.Add(pair.Key);
                }

                UpdateThumbnail();
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

            Panning = false;
            Selecting = false;

            SelectedNodes_CollectionChanged(null, null);
        }
        private void Canvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta > 0)
                    Scale = (Math.Floor(Scale * 4) + 1) / 4.0;
                else
                    Scale = (Math.Ceiling(Scale * 4) - 1) / 4.0;
            }
            else
                Scale *= 1 + e.Delta / 120 * 0.1;
        }
        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FlowTomator.Node") || sender == e.Source)
                e.Effects = DragDropEffects.None;
        }
        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            Type nodeType = e.Data.GetData("FlowTomator.Node") as Type;

            Point mousePosition = e.GetPosition(EditorCanvas);

            // Create new node
            Node node = Activator.CreateInstance(nodeType) as Node;
            node.Metadata.Add("Position.X", mousePosition.X);
            node.Metadata.Add("Position.Y", mousePosition.Y);

            FlowInfo.History.Do(new AddNodeAction(FlowInfo, node));
        }
        private void Canvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                FlowInfo.History.Do(new ActionGroup(SelectedNodes.Select(n => new DeleteNodeAction(n))));
        }

        private void RemoveLinkItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            LinkInfo linkInfo = menuItem.Tag as LinkInfo;

            FlowInfo.History.Do(new DeleteLinkAction(linkInfo));
        }

        private void UpdateThumbnail()
        {
            //EditorThumbnail.GetBindingExpression(Image.SourceProperty).UpdateTarget();
            //EditorThumbnail.UpdateLayout();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));

            UpdateThumbnail();
        }
    }
}