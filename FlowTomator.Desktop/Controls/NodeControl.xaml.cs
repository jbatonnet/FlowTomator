using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class NodeControl : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty NodeInfoProperty = DependencyProperty.Register(nameof(NodeInfo), typeof(NodeInfo), typeof(NodeControl), new PropertyMetadata(NodeControl_NodeChanged));

        public NodeInfo NodeInfo { get; set; }

        public Visibility InputSlotVisibility { get { return NodeInfo != null && !(NodeInfo.Node is Origin) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility InputsVisibility { get { return NodeInfo != null && NodeInfo.Node.Inputs.Any() ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility OutputsVisibility { get { return NodeInfo != null && NodeInfo.Node.Outputs.Any() ? Visibility.Visible : Visibility.Collapsed; } }

        public event MouseButtonEventHandler HeaderMouseDown;
        public event MouseButtonEventHandler SlotMouseDown;
        public event PropertyChangedEventHandler PropertyChanged;

        public NodeControl()
        {
            InitializeComponent();
            Root.DataContext = this;
        }

        private void NodeControl_Loaded(object sender, RoutedEventArgs e)
        {
            Root.DataContext = this;
        }
        private static void NodeControl_NodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NodeControl me = d as NodeControl;
            me.NodeInfo = e.NewValue as NodeInfo;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (HeaderMouseDown != null)
                HeaderMouseDown(this, e);
        }
        private void VariableValue_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            VariableInfo variableInfo = textBlock.DataContext as VariableInfo;

            foreach (NodeInfo nodeInfo in NodeInfo.FlowInfo.Nodes)
                foreach (VariableInfo variable in nodeInfo.Inputs)
                    variable.Selected = false;

            variableInfo.Selected = true;
        }
        private void Slot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            ContentPresenter contentPresenter = VisualTreeHelper.GetParent(border) as ContentPresenter;
            SlotInfo slotInfo = Slots.ItemContainerGenerator.ItemFromContainer(contentPresenter) as SlotInfo;

            if (SlotMouseDown != null)
                SlotMouseDown(slotInfo, e);
        }

        private void ResetDefaultValueItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            VariableInfo variableInfo = menuItem.Tag as VariableInfo;

            variableInfo.Variable.Link(null);
            variableInfo.Value = variableInfo.Variable.DefaultValue;
        }

        private void Variable_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FlowTomator.Variable") || sender == e.Source)
                e.Effects = DragDropEffects.None;
        }
        private void Variable_Drop(object sender, DragEventArgs e)
        {
            VariableInfo dropVariableInfo = e.Data.GetData("FlowTomator.Variable") as VariableInfo;

            FrameworkElement textBox = sender as FrameworkElement;
            ContentPresenter contentPresenter = textBox.TemplatedParent as ContentPresenter;

            VariableInfo variableInfo = contentPresenter.Content as VariableInfo;
            NodeInfo.FlowInfo.History.Do(new LinkVariableAction(variableInfo, dropVariableInfo.Variable));
        }
    }
}