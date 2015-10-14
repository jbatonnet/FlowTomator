using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class LinkControl : UserControl
    {
        public static DependencyProperty SlotProperty = DependencyProperty.Register("Slot", typeof(Slot), typeof(LinkControl), new PropertyMetadata(LinkControl_SlotChanged));
        public static DependencyProperty NodeProperty = DependencyProperty.Register("Node", typeof(Node), typeof(LinkControl), new PropertyMetadata(LinkControl_NodeChanged));

        public Slot Slot { get; set; }
        public Node Node { get; set; }

        public LinkControl()
        {
            InitializeComponent();
        }

        
        public void Refresh()
        {
            DependencyObject tabCanvasObject = VisualParent;
            while (!(tabCanvasObject is ItemsControl))
                tabCanvasObject = VisualTreeHelper.GetParent(tabCanvasObject);

            Canvas tabCanvas = VisualTreeHelper.GetParent(tabCanvasObject) as Canvas;
            ItemsControl tabNodes = VisualTreeHelper.GetChild(tabCanvas, 0) as ItemsControl;

            int sourceSlotIndex = -1;
            NodeControl sourceNodeControl = tabNodes.Items.OfType<NodeInfo>()
                                                          .Select(n => tabNodes.ItemContainerGenerator.ContainerFromItem(n) as ContentPresenter)
                                                          .Select(c => VisualTreeHelper.GetChild(c, 0) as NodeControl)
                                                          .First(n => (sourceSlotIndex = n.NodeInfo.Node.Slots.ToList().IndexOf(Slot)) >= 0);

            Point sourceNodePosition = sourceNodeControl.TransformToAncestor(tabCanvas).Transform(new Point(0, 0));
            int sourceSlotCount = sourceNodeControl.NodeInfo.Node.Slots.Count();

            double left = sourceNodePosition.X + 128 - (sourceSlotCount * 2 - 1) * 12;
            Line.X1 = left + (sourceSlotIndex * 2 - 1) * 12 + 6;
            Line.Y1 = sourceNodePosition.Y + 10 - 6;
            
        }

        private void LinkControl_Loaded(object sender, RoutedEventArgs e)
        {
            Root.DataContext = this;
        }

        private static void LinkControl_SlotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkControl me = d as LinkControl;
            me.Slot = e.NewValue as Slot;
            me.Refresh();
        }
        private static void LinkControl_NodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkControl me = d as LinkControl;
            me.Node = e.NewValue as Node;
            me.Refresh();
        }
    }
}
