using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private NodeControl movingNodeControl;
        private Point movingNodeOrigin;
        private Point movingNodeControlOffset;
        private int currentZIndex = 1;

        private bool creatingNewLink = false;
        private SlotInfo newLinkSlot;

        public XFlowEditor()
        {
            Tag = new DependencyManager(this, (s, e) => PropertyChanged(s, e));
            InitializeComponent();

            DataContextChanged += XFlowEditor_DataContextChanged;
        }

        private void XFlowEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FlowInfo.PropertyChanged += FlowInfo_PropertyChanged;
        }
        private void FlowInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //EditorThumbnail.GetBindingExpression(Image.SourceProperty).UpdateSource();
        }

        private void NodeControl_HeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            NodeControl nodeControl = movingNodeControl = sender as NodeControl;
            ContentPresenter contentPresenter = VisualTreeHelper.GetParent(nodeControl) as ContentPresenter;
            Canvas canvas = VisualTreeHelper.GetParent(contentPresenter) as Canvas;

            movingNodeOrigin = new Point(nodeControl.NodeInfo.X, nodeControl.NodeInfo.Y);
            movingNodeControlOffset = e.GetPosition(nodeControl);

            contentPresenter.SetValue(Panel.ZIndexProperty, currentZIndex++);
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

            foreach (NodeInfo nodeInfo in FlowInfo.Nodes)
                foreach (VariableInfo variable in nodeInfo.Inputs)
                    variable.Selected = false;
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            Canvas canvas = sender as Canvas;

            if (movingNodeControl != null)
            {
                ContentPresenter contentPresenter = VisualTreeHelper.GetParent(movingNodeControl) as ContentPresenter;

                Point mousePosition = e.GetPosition(canvas);
                Point nodeControlPosition = new Point(mousePosition.X - movingNodeControlOffset.X, mousePosition.Y - movingNodeControlOffset.Y);

                nodeControlPosition.X = Math.Round(nodeControlPosition.X / 4) * 4;
                nodeControlPosition.Y = Math.Round(nodeControlPosition.Y / 4) * 4;

                contentPresenter.SetValue(Canvas.LeftProperty, nodeControlPosition.X);
                contentPresenter.SetValue(Canvas.TopProperty, nodeControlPosition.Y);
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
            if (movingNodeControl != null)
            {
                FlowInfo.History.Do(new MoveNodeAction(movingNodeControl.NodeInfo, movingNodeOrigin, new Point(movingNodeControl.NodeInfo.X, movingNodeControl.NodeInfo.Y)));
                movingNodeControl = null;
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