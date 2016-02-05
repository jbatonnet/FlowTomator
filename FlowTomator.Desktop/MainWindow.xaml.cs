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
using System.Windows.Data;
using FlowTomator.Desktop.Properties;

namespace FlowTomator.Desktop
{
    public class LogMessage
    {
        public DateTime Date { get; private set; }
        public LogCategory Category { get; private set; }
        public LogVerbosity Verbosity { get; private set; }
        public string Message { get; private set; }

        public LogMessage(DateTime date, LogCategory category, LogVerbosity verbosity, string message)
        {
            Date = date;
            Category = category;
            Verbosity = verbosity;
            Message = message;
        }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<FlowInfo> Flows { get; } = new ObservableCollection<FlowInfo>();
        public ObservableCollection<NodeCategoryInfo> NodeCategories { get; } = new ObservableCollection<NodeCategoryInfo>();

        public ObservableDictionary<string, ObservableCollection<LogMessage>> LogMessages { get; } = new ObservableDictionary<string, ObservableCollection<LogMessage>>();
        public LogVerbosity LogVerbosity => Log.Verbosity;

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

        [DependsOn(nameof(CurrentFlow))]
        public ICollection<VariableInfo> CurrentVariables
        {
            get
            {
                return CurrentFlow?.Variables;
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

        public int LogSelectedCategory
        {
            get
            {
                return logSelectedCategory;
            }
            set
            {
                logSelectedCategory = value;
                NotifyPropertyChanged();
            }
        }
        private int logSelectedCategory;

        public bool LogAutoscroll
        {
            get
            {
                return Settings.Default.LogAutoscroll;
            }
            set
            {
                Settings.Default.LogAutoscroll = value;
                Settings.Default.Save();

                NotifyPropertyChanged();
            }
        }
        public bool LogAutoswitch
        {
            get
            {
                return Settings.Default.LogAutoswitch;
            }
            set
            {
                Settings.Default.LogAutoswitch = value;
                Settings.Default.Save();

                NotifyPropertyChanged();
            }
        }

        private bool draggingNode = false;
        private bool draggingVariable = false;

        private List<LogMessage> logBuffer = new List<LogMessage>();
        private DispatcherTimer logTimer;
        //private Dictionary<>

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
            Log.Verbosity = LogVerbosity.Trace;
            LogMessages.Add(LogCategory.Common.Name, new ObservableCollection<LogMessage>());
            LogMessages.Add(FlowDebugger.DebuggerCategory.Name, new ObservableCollection<LogMessage>());
            logTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.ApplicationIdle, Log_TimerCallback, Dispatcher.CurrentDispatcher);
            Log.Message += Log_Message;

            // Analyze loaded assemblies
            AppDomain.CurrentDomain.AssemblyLoad += (s, a) => AnalyzeAssembly(a.LoadedAssembly);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                AnalyzeAssembly(assembly);

            // Load referenced assemblies
            if (Settings.Default.Assemblies != null)
            {
                foreach (string path in Settings.Default.Assemblies)
                {
                    string assemblyPath = path;

                    if (!Path.IsPathRooted(assemblyPath))
                        assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyPath);

                    try
                    {
                        Assembly.LoadFrom(assemblyPath);
                    }
                    catch { }
                }
            }

            DataContext = this;
            InitializeComponent();

            // Load specified arguments
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args.Skip(1))
                Open(arg);

            Log.Info("FlowTomator is ready");
        }

        private void AnalyzeAssembly(Assembly assembly)
        {
            if (assembly.ReflectionOnly)
                return;

            IEnumerable<Type> nodeTypes = Enumerable.Empty<Type>();
            
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

        private void Log_Message(LogVerbosity verbosity, LogCategory category, string message)
        {
            DateTime date = DateTime.Now;

            lock (logBuffer)
                logBuffer.Add(new LogMessage(date, category, verbosity, message));
        }
        private void Log_TimerCallback(object sender, EventArgs e)
        {
            if (logBuffer.Count == 0)
                return;

            LogMessage[] logMessages;

            lock (logBuffer)
            {
                logMessages = logBuffer.ToArray();
                if (logMessages.Length == 0)
                    return;

                logBuffer.Clear();
            }

            int newIndex = -1;

            foreach (LogMessage logMessage in logMessages)
            {
                int i = 0;
                ObservableCollection<LogMessage> categoryMessages = null;

                for (; i < LogMessages.Count; i++)
                    if (LogMessages.ElementAt(i).Key == logMessage.Category.Name)
                    {
                        categoryMessages = LogMessages.ElementAt(i).Value;
                        break;
                    }

                if (i == LogMessages.Count)
                {
                    LogMessages.Add(logMessage.Category.Name, categoryMessages = new ObservableCollection<LogMessage>());
                    NotifyPropertyChanged(nameof(LogMessages));
                }

                if (logMessage.Verbosity >= Log.Verbosity)
                    newIndex = i;

                categoryMessages.Add(logMessage);
            }

            //if (LogAutoswitch && newIndex >= 0)
            //    LogSelectedCategory = newIndex;
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
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                UndoCommand.Execute(null);
            else if (e.Key == Key.Y && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                RedoCommand.Execute(null);

            else if (e.Key == Key.N && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                NewFlowCommand.Execute(null);
            else if (e.Key == Key.O && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                OpenFlowCommand.Execute(null);
            else if (e.Key == Key.S && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift))
                SaveAllFlowsCommand.Execute(null);
            else if (e.Key == Key.S && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                SaveFlowCommand.Execute(null);

            else if (e.Key == Key.F5 && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
                StopFlowCommand.Execute(null);
            else if (e.Key == Key.F5)
                RunFlowCommand.Execute(null);
            else if (e.Key == Key.F10)
                StepFlowCommand.Execute(null);
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
            Log.Info("Opening \"{0}\"", path);

            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                MessageBox.Show("Could not find specified file " + path, "FlowTomator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FlowInfo flowInfo = null;
            string error = "";

            try
            {
                switch (fileInfo.Extension)
                {
                    case ".xflow": flowInfo = new FlowInfo(XFlow.Load(XDocument.Load(fileInfo.FullName, LoadOptions.SetLineInfo)), fileInfo.FullName); break;
                }
            }
            catch (Exception e)
            {
                error = e.Message;

                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
            }

            if (flowInfo == null || flowInfo.Flow == null)
            {
                MessageBox.Show("Could not open specified file " + path + "." + Environment.NewLine + error, "FlowTomator", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    if (string.IsNullOrEmpty(flowInfo.Path) || !File.Exists(flowInfo.Path))
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();

                        saveFileDialog.Filter = "XFlow files (*.xflow)|*.xflow";
                        saveFileDialog.FilterIndex = 1;

                        if (saveFileDialog.ShowDialog() != true)
                            return;

                        flowInfo.Path = saveFileDialog.FileName;
                    }

                    document.Save(flowInfo.Path);
                    Log.Info("Saved flow to \"{0}\"", flowInfo.Path);
                }

                flowInfo.History.Clear();
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred while saving your flow. " + e);
            }
        }

        private void RunFlowCommandCallback(object parameter)
        {
            if (Debugger == null)
                Debugger = new FlowDebugger(CurrentFlow);

            if (Debugger.State == DebuggerState.Idle)
                LogMessages.ElementAt(1).Value.Clear();

            Debugger.Run();
        }
        private void StepFlowCommandCallback(object parameter)
        {
            if (Debugger == null)
                Debugger = new FlowDebugger(CurrentFlow);

            if (Debugger.State == DebuggerState.Idle)
                LogMessages.ElementAt(1).Value.Clear();

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

            NodeTypeInfo nodeTypeInfo = contentPresenter.Content as NodeTypeInfo;
            DataObject dragData = new DataObject("FlowTomator.Node", nodeTypeInfo.Type);
            DragDrop.DoDragDrop(grid, dragData, DragDropEffects.Move);

            draggingNode = false;
        }
        private void VariableList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            draggingVariable = true;
        }
        private void VariableList_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingVariable || e.LeftButton != MouseButtonState.Pressed)
                return;

            Grid grid = sender as Grid;
            ContentPresenter contentPresenter = VisualTreeHelper.GetParent(grid) as ContentPresenter;

            DependencyObject itemsControl = contentPresenter;
            while (!(itemsControl is ItemsControl))
                itemsControl = VisualTreeHelper.GetParent(itemsControl);

            VariableInfo variableInfo = contentPresenter.Content as VariableInfo;
            DataObject dragData = new DataObject("FlowTomator.Variable", variableInfo);
            DragDrop.DoDragDrop(grid, dragData, DragDropEffects.Move);

            draggingVariable = false;
        }
        private void VariableValue_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            VariableInfo variableInfo = textBlock.DataContext as VariableInfo;

            foreach (VariableInfo variable in CurrentFlow.Variables)
                variable.Selected = false;

            variableInfo.Selected = true;
        }

        private void Menu_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as FrameworkElement).ContextMenu.IsOpen = true;
        }
        private void ReloadNodesButton_Click(object sender, EventArgs e)
        {
            NodeCategories.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                AnalyzeAssembly(assembly);
        }
        private void ManageNodesButton_Click(object sender, EventArgs e)
        {
            ReferencesWindow references = new ReferencesWindow();

            references.Owner = this;
            references.ShowDialog();
        }

        private void AddVariableButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentVariables.Add(VariableInfo.From(CurrentFlow, new Variable("New")));
        }

        private void ClearCurrentLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogMessages.ElementAt(LogSelectedCategory).Value.Clear();
        }
        private void ClearAllLogButton_Click(object sender, RoutedEventArgs e)
        {
            while (LogMessages.Count > 2)
                LogMessages.Remove(LogMessages.Keys.ElementAt(1));
            NotifyPropertyChanged(nameof(LogMessages));

            LogMessages[LogCategory.Common.Name].Clear();
            LogSelectedCategory = 0;
        }
        private void TestLogButton_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    foreach (LogCategory category in new[] { LogCategory.Common, new LogCategory("Debugger"), new LogCategory("Node") })
                        for (int i = 0; i < 5; i++)
                        {
                            Log.Trace(category, "{0}.Trace", category.Name);
                            Log.Debug(category, "{0}.Debug", category.Name);
                            Log.Info(category, "{0}.Info", category.Name);
                            Log.Warning(category, "{0}.Warning", category.Name);
                            Log.Error(category, "{0}.Error", category.Name);
                        }
                }
            }).Start();
        }
        private void SetLogVerbosityButton_Click(object sender, RoutedEventArgs e)
        {
            string verbosityText = (sender as MenuItem).Header.ToString();
            LogVerbosity verbosity = (LogVerbosity)Enum.Parse(typeof(LogVerbosity), verbosityText);

            Log.Verbosity = verbosity;
            NotifyPropertyChanged(nameof(LogVerbosity));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}