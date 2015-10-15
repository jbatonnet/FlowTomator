using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

using Microsoft.Win32;

using FlowTomator.Common;
using System.Runtime.CompilerServices;

namespace FlowTomator.Desktop
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<FlowInfo> Flows { get; } = new ObservableCollection<FlowInfo>();
        public ObservableCollection<NodeCategoryInfo> NodeCategories { get; } = new ObservableCollection<NodeCategoryInfo>();

        public DelegateCommand NewFlowCommand { get; private set; }
        public DelegateCommand OpenFlowCommand { get; private set; }

        [DependsOn(nameof(CurrentHistory))]
        public DelegateCommand SaveFlowCommand { get; private set; }
        [DependsOn(nameof(Flows), nameof(CurrentHistory))]
        public DelegateCommand SaveAllFlowsCommand { get; private set; }

        [DependsOn(nameof(CurrentHistory))]
        public DelegateCommand UndoCommand { get; private set; }
        [DependsOn(nameof(CurrentHistory))]
        public DelegateCommand RedoCommand { get; private set; }
        
        public DelegateCommand RunFlowCommand { get; private set; }
        public DelegateCommand StepFlowCommand { get; private set; }
        public DelegateCommand BreakFlowCommand { get; private set; }
        public DelegateCommand StopFlowCommand { get; private set; }

        [DependsOn(nameof(Flows))]
        public FlowInfo CurrentFlow
        {
            get
            {
                return currentFlow;
            }
            set
            {
                currentFlow = value;
                NotifyPropertyChanged();
            }
        }
        private FlowInfo currentFlow;

        [DependsOn(nameof(CurrentFlow))]
        public HistoryInfo CurrentHistory
        {
            get
            {
                return CurrentFlow?.History;
            }
        }

        private FlowInfo DebuggedFlow;
        private List<NodeInfo> DebuggedNodes = new List<NodeInfo>();
        private bool DebuggerPaused = false;
        private bool DebuggerStepping = false;
        private Thread DebuggerThread = null;

        public MainWindow()
        {
            Tag = new DependencyManager(this, (s, e) => PropertyChanged(s, e));
            System.Windows.Forms.Application.EnableVisualStyles();

            // Create commands
            NewFlowCommand = new DelegateCommand(NewFlowCommandCallback);
            OpenFlowCommand = new DelegateCommand(OpenFlowCommandCallback);
            SaveFlowCommand = new DelegateCommand(SaveFlowCommandCallback, p => CurrentHistory?.Actions?.Any() == true);
            SaveAllFlowsCommand = new DelegateCommand(SaveAllFlowsCommandCallback, p => Flows.Any(f => f.History.Actions.Any()));
            UndoCommand = new DelegateCommand(UndoCommandCallback, p => CurrentHistory?.CanUndo == true);
            RedoCommand = new DelegateCommand(RedoCommandCallback, p => CurrentHistory?.CanRedo == true);
            RunFlowCommand = new DelegateCommand(RunFlowCommandCallback, p => !DebuggerStepping && (DebuggerPaused || DebuggedFlow == null));
            StepFlowCommand = new DelegateCommand(StepFlowCommandCallback, p => !DebuggerStepping && (DebuggerPaused || DebuggedFlow == null));
            BreakFlowCommand = new DelegateCommand(BreakFlowCommandCallback, p => !DebuggerPaused && DebuggedFlow != null);
            StopFlowCommand = new DelegateCommand(StopFlowCommandCallback, p => DebuggedFlow != null);

            // Analyze loaded assemblies
            AppDomain.CurrentDomain.AssemblyLoad += (s, a) => AnalyzeAssembly(a.LoadedAssembly);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                AnalyzeAssembly(assembly);

            DataContext = this;
            InitializeComponent();

            // Load specified arguments
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args.Skip(1))
                Open(arg);
        }

        private void AnalyzeAssembly(Assembly assembly)
        {
            IEnumerable<Type> nodeTypes = Enumerable.Empty<Type>();

            //nodeTypes = assembly.GetModules().SelectMany(m => m.GetTypes());
            nodeTypes = assembly.GetTypes();

            nodeTypes = nodeTypes.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)) && t != typeof(Flow) && !t.IsSubclassOf(typeof(Flow)))
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
                {
                    e.Cancel = true;
                    return;
                }

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    foreach (FlowInfo flowInfo in unsavedFlows)
                        Save(flowInfo);
                }
            }

            StopFlowCommand.Execute(null);
        }

        private void NewFlowCommandCallback(object parameter)
        {
            throw new NotImplementedException();
        }
        private void OpenFlowCommandCallback(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XFlow files (*.xml)|*.xml";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() != true)
                return;

            Open(openFileDialog.FileName);
        }
        private void SaveFlowCommandCallback(object parameter)
        {
            if (CurrentFlow != null)
                Save(CurrentFlow);
        }
        private void SaveAllFlowsCommandCallback(object parameter)
        {
            foreach (FlowInfo flowInfo in Flows)
                if (flowInfo.History.Actions.Any())
                    Save(flowInfo);
        }
        private void UndoCommandCallback(object parameter)
        {
            if (CurrentHistory != null)
                CurrentHistory.Undo();
        }
        private void RedoCommandCallback(object parameter)
        {
            if (CurrentHistory != null)
                CurrentHistory.Redo();
        }

        public void Open(string path)
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
            catch
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }

            if (flowInfo.Flow == null)
            {
                MessageBox.Show("Could not open specified file " + path, "FlowTomator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Flows.Add(flowInfo);
            NotifyPropertyChanged(nameof(Flows));
        }
        public void Save(FlowInfo flowInfo)
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

        private void RunFlowCommandCallback(object parameter)
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

            StepFlowCommandCallback(parameter);
        }
        private void StepFlowCommandCallback(object parameter)
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

                                                         NodeStep nodeStep = new NodeStep(NodeResult.Fail, null);
                                                         try
                                                         {
                                                             nodeStep = ni.Node.Evaluate();
                                                         }
                                                         catch { }

                                                         if (DebuggedFlow == null)
                                                             return Enumerable.Empty<NodeInfo>();

                                                         ni.Status = NodeStatus.Idle;
                                                         ni.Result = nodeStep.Result;

                                                         if (ni.Result == NodeResult.Stop || ni.Result == NodeResult.Fail)
                                                         {
                                                             DebuggerPaused = true;
                                                             return Enumerable.Empty<NodeInfo>();
                                                         }

                                                         NodeInfo[] nodeInfos = nodeStep.Slot == null ? new NodeInfo[0] : nodeStep.Slot.Nodes.Select(n => NodeInfo.From(flowInfo, n)).ToArray();
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
                    StepFlowCommandCallback(parameter);
            });

            DebuggerThread.Start();
        }
        private void BreakFlowCommandCallback(object parameter)
        {
            DebuggerPaused = true;
            RefreshDebuggerUI();
        }
        private void StopFlowCommandCallback(object parameter)
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
                RunFlowCommand.PropertyUpdate();
                StepFlowCommand.PropertyUpdate();
                BreakFlowCommand.PropertyUpdate();
                StopFlowCommand.PropertyUpdate();
            }
        }

        private void NodeList_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
        private void NodeList_MouseMove(object sender, MouseEventArgs e)
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}