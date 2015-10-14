using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using System.Xml.Linq;

using FlowTomator.Common;
using FlowTomator.Common.Nodes;
using Microsoft.Win32;

namespace FlowTomator.Desktop
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<NodeCategoryInfo> NodeCategories { get; } = new ObservableCollection<NodeCategoryInfo>();
        public ObservableCollection<FlowInfo> Flows { get; } = new ObservableCollection<FlowInfo>();

        public bool RunButtonEnabled
        {
            get
            {
                return !DebuggerStepping && (DebuggerPaused || DebuggedFlow == null);
            }
        }
        public bool StepButtonEnabled
        {
            get
            {
                return !DebuggerStepping && (DebuggerPaused || DebuggedFlow == null);
            }
        }
        public bool PauseButtonEnabled
        {
            get
            {
                return !DebuggerPaused && DebuggedFlow != null;
            }
        }
        public bool StopButtonEnabled
        {
            get
            {
                return DebuggedFlow != null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private FlowInfo DebuggedFlow;
        private List<NodeInfo> DebuggedNodes = new List<NodeInfo>();
        private bool DebuggerPaused = false;
        private bool DebuggerStepping = false;
        private Thread DebuggerThread = null;

        public MainWindow()
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            // Load assemblies
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                BrowseAssembly(assembly);

            DataContext = this;
            InitializeComponent();

            // Load specified arguments
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args.Skip(1))
                TryOpen(arg);
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            BrowseAssembly(args.LoadedAssembly);
        }
        private void BrowseAssembly(Assembly assembly)
        {
            IEnumerable<Type> nodeTypes = assembly.GetTypes()
                                                  .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)) && t != typeof(Flow) && !t.IsSubclassOf(typeof(Flow)))
                                                  .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

            foreach (Type type in nodeTypes)
            {
                NodeTypeInfo nodeType = NodeTypeInfo.From(type);
                NodeCategoryInfo nodeCategory = NodeCategories.FirstOrDefault(c => c.Category == nodeType.Category);

                if (nodeCategory == null)
                    NodeCategories.Add(nodeCategory = new NodeCategoryInfo(nodeType.Category));

                nodeCategory.Nodes.Add(nodeType);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Tabs.SelectedIndex = 0;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            FlowInfo[] unsavedFlows = Flows.Where(f => f.History.Actions.Any())
                                           .ToArray();

            if (unsavedFlows.Length > 0)
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show("There are unsaved changed to open flows. Do you want to save your modifications before exiting ?", "FlowTomator", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    foreach (FlowInfo flowInfo in unsavedFlows)
                        TrySave(flowInfo);
                }
            }

            if (StopButtonEnabled)
                StopButton_Click(sender, new RoutedEventArgs());
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XFlow files (*.xml)|*.xml";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() != true)
                return;

            TryOpen(openFileDialog.FileName);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FlowInfo flowInfo = Tabs.SelectedItem as FlowInfo;
            TrySave(flowInfo);
        }

        public void TryOpen(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                MessageBox.Show("Could not find specified file " + path, "FlowTomator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FlowInfo flowInfo = null;

            try
            {
                switch (fileInfo.Extension)
                {
                    case ".xml": flowInfo = new FlowInfo(XFlow.Load(XDocument.Load(fileInfo.FullName)), fileInfo.FullName); break;
                }
            }
            catch { }

            if (flowInfo == null)
            {
                MessageBox.Show("Could not open specified file " + path, "FlowTomator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Flows.Add(flowInfo);
        }
        public void TrySave(FlowInfo flowInfo)
        {
            Type flowType = flowInfo.Flow.GetType();

            try
            {
                if (flowType == typeof(XFlow))
                    XFlow.Save(flowInfo.Flow as XFlow).Save(flowInfo.Path);

                flowInfo.History.Clear();
            }
            catch { }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Grid grid = sender as Grid;
            ContentPresenter contentPresenter = VisualTreeHelper.GetParent(grid) as ContentPresenter;

            DependencyObject itemsControl = contentPresenter;
            while (!(itemsControl is ItemsControl))
                itemsControl = VisualTreeHelper.GetParent(itemsControl);

            NodeTypeInfo nodeTypeInfo = (itemsControl as ItemsControl).ItemContainerGenerator.ItemFromContainer(contentPresenter) as NodeTypeInfo;

            DataObject dragData = new DataObject("FlowTomator.Node", nodeTypeInfo.Type);
            DragDrop.DoDragDrop(grid, dragData, DragDropEffects.Move);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            FlowInfo flowInfo = Tabs.SelectedItem as FlowInfo;

            DebuggedFlow = flowInfo;
            DebuggedNodes = flowInfo.Flow.Origins.OfType<Node>()
                                                 .Select(n => NodeInfo.From(flowInfo, n))
                                                 .ToList();

            foreach (NodeInfo nodeInfo in flowInfo.Nodes)
                nodeInfo.Result = NodeResult.Success;
            foreach (NodeInfo nodeInfo in DebuggedNodes)
                nodeInfo.Status = NodeStatus.Paused;

            DebuggerPaused = false;
            RefreshDebuggerUI();

            StepButton_Click(sender, e);
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            DebuggerPaused = true;
            RefreshDebuggerUI();
        }
        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            FlowInfo flowInfo = null;
            Dispatcher.Invoke(() => flowInfo = Tabs.SelectedItem as FlowInfo);

            if (DebuggedFlow == null)
            {
                DebuggedFlow = flowInfo;
                DebuggedNodes = flowInfo.Flow.Origins.OfType<Node>()
                                                     .Select(n => NodeInfo.From(flowInfo, n))
                                                     .ToList();

                foreach (NodeInfo nodeInfo in DebuggedNodes)
                    nodeInfo.Status = NodeStatus.Paused;

                DebuggerPaused = true;
            }

            DebuggerThread = new Thread(() =>
            {
                lock (DebuggedNodes)
                {
                    if (DebuggedNodes.Count == 0)
                    {
                        DebuggedFlow = null;
                        DebuggerPaused = true;

                        RefreshDebuggerUI();
                        return;
                    }

                    DebuggerStepping = true;
                    RefreshDebuggerUI();

                    try
                    {
                        DebuggedNodes = DebuggedNodes.AsParallel()
                                                     .SelectMany(ni =>
                                                     {
                                                         ni.Status = NodeStatus.Running;
                                                         NodeStep nodeStep = ni.Node.Evaluate();

                                                         if (DebuggedFlow == null)
                                                             return Enumerable.Empty<NodeInfo>();

                                                         ni.Status = NodeStatus.Idle;
                                                         ni.Result = nodeStep.Result;

                                                         if (ni.Result == NodeResult.Stop || ni.Result == NodeResult.Fail)
                                                             DebuggerPaused = true;

                                                         if (ni.Result == NodeResult.Fail)
                                                             return Enumerable.Empty<NodeInfo>();

                                                         List<NodeInfo> nodeInfos = nodeStep.Slot.Nodes.Select(n => NodeInfo.From(flowInfo, n)).ToList();
                                                         foreach (NodeInfo nodeInfo in nodeInfos)
                                                             nodeInfo.Status = NodeStatus.Paused;

                                                         return nodeInfos;
                                                     })
                                                     .ToList();
                    }
                    catch { }

                    DebuggerStepping = false;
                    RefreshDebuggerUI();
                }

                if (DebuggedNodes.Count == 0)
                {
                    DebuggedFlow = null;
                    DebuggerPaused = true;

                    RefreshDebuggerUI();
                    return;
                }

                if (!DebuggerPaused)
                    StepButton_Click(sender, e);
            });

            DebuggerThread.Start();
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            FlowInfo flowInfo = Tabs.SelectedItem as FlowInfo;

            DebuggedFlow = null;
            DebuggerPaused = true;
            DebuggerStepping = false;

            if (DebuggerThread != null)
            {
                DebuggerThread.Abort();
                DebuggerThread = null;
            }

            foreach (NodeInfo nodeInfo in flowInfo.Nodes)
                nodeInfo.Status = NodeStatus.Idle;

            RefreshDebuggerUI();
        }
        private void RefreshDebuggerUI()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(RunButtonEnabled)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(StepButtonEnabled)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(PauseButtonEnabled)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(StopButtonEnabled)));
            }
        }
    }
}