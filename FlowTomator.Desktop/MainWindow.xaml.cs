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
using System.Text;
using System.Windows.Threading;

namespace FlowTomator.Desktop
{
    public class LogTextWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.Default;
            }
        }
        public event Action<string> Updated;

        public override void Write(char[] buffer, int index, int count)
        {
            string text = new string(buffer, index, count);

            if (Updated != null)
                Updated(text);
        }
    }

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
        
        [DependsOn(nameof(Debugger))]
        public DelegateCommand RunFlowCommand { get; private set; }
        [DependsOn(nameof(Debugger))]
        public DelegateCommand StepFlowCommand { get; private set; }
        [DependsOn(nameof(Debugger))]
        public DelegateCommand BreakFlowCommand { get; private set; }
        [DependsOn(nameof(Debugger))]
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

        public FlowDebugger Debugger
        {
            get
            {
                return debugger;
            }
            set
            {
                debugger = value;
                NotifyPropertyChanged();
            }
        }
        private FlowDebugger debugger;

        public string Output
        {
            get
            {
                return outBuilder.ToString();
            }
            set { }
        }
        private LogTextWriter outRedirector = new LogTextWriter();
        private StringBuilder outBuilder = new StringBuilder();

        private bool draggingNode;

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
            RunFlowCommand = new DelegateCommand(RunFlowCommandCallback, p => Debugger?.State != DebuggerState.Running);
            StepFlowCommand = new DelegateCommand(StepFlowCommandCallback, p => Debugger?.State != DebuggerState.Running);
            BreakFlowCommand = new DelegateCommand(BreakFlowCommandCallback, p => Debugger?.State == DebuggerState.Running);
            StopFlowCommand = new DelegateCommand(StopFlowCommandCallback, p => (Debugger?.State ?? DebuggerState.Idle) != DebuggerState.Idle);

            // Redirect console output
            Console.SetOut(outRedirector);
            outRedirector.Updated += OutRedirector_Updated;
            Log.Verbosity = LogVerbosity.Trace;

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
        private void OutRedirector_Updated(string obj)
        {
            outBuilder.Append(obj);
            NotifyPropertyChanged(nameof(Output));

            Dispatcher.Invoke(() =>
            {
                LogOutput.ScrollToEnd();
            }, DispatcherPriority.Background);
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
            XFlow flow = new XFlow();
            FlowInfo flowInfo = new FlowInfo(flow, null);

            Flows.Add(flowInfo);
            CurrentFlow = flowInfo;
        }
        private void OpenFlowCommandCallback(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XFlow files (*.xflow)|*.xflow";
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
                    case ".xflow": flowInfo = new FlowInfo(XFlow.Load(XDocument.Load(fileInfo.FullName)), fileInfo.FullName); break;
                }
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached)
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
                {
                    XDocument document = XFlow.Save(flowInfo.Flow as XFlow);

                    /*if (string.IsNullOrEmpty(flowInfo.Path) || !File.Exists(flowInfo.Path))
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();

                        saveFileDialog
                    }*/

                    document.Save(flowInfo.Path);
                }

                flowInfo.History.Clear();
            }
            catch { }
        }

        private void RunFlowCommandCallback(object parameter)
        {
            if (Debugger == null)
                Debugger = new FlowDebugger(CurrentFlow);

            Debugger.Run();
        }
        private void StepFlowCommandCallback(object parameter)
        {
            if (Debugger == null)
                Debugger = new FlowDebugger(CurrentFlow);

            Debugger.Step();
        }
        private void BreakFlowCommandCallback(object parameter)
        {
            if (Debugger == null)
                return;

            Debugger.Break();
        }
        private void StopFlowCommandCallback(object parameter)
        {
            if (Debugger == null)
                return;

            Debugger.Stop();

            foreach (NodeInfo nodeInfo in CurrentFlow.Nodes)
            {
                nodeInfo.Status = NodeStatus.Idle;
                nodeInfo.Result = NodeResult.Success;
            }
        }

        private void NodeList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            draggingNode = true;
        }
        private void NodeList_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingNode || e.LeftButton != MouseButtonState.Pressed)
                return;

            Grid grid = sender as Grid;
            ContentPresenter contentPresenter = VisualTreeHelper.GetParent(grid) as ContentPresenter;

            DependencyObject itemsControl = contentPresenter;
            while (!(itemsControl is ItemsControl))
                itemsControl = VisualTreeHelper.GetParent(itemsControl);

            NodeTypeInfo nodeTypeInfo = (itemsControl as ItemsControl).ItemContainerGenerator.ItemFromContainer(contentPresenter) as NodeTypeInfo;

            DataObject dragData = new DataObject("FlowTomator.Node", nodeTypeInfo.Type);
            DragDrop.DoDragDrop(grid, dragData, DragDropEffects.Move);

            draggingNode = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}