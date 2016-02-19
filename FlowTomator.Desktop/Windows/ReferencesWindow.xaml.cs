using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FlowTomator.Common;
using FlowTomator.Desktop.Properties;
using Microsoft.Win32;

namespace FlowTomator.Desktop
{
    public class AssemblyInfo
    {
        public bool Selected { get; set; }
        public bool Enabled { get; set; }

        public string Name { get; private set; }
        public string Path { get; private set; }

        public AssemblyInfo(Assembly assembly, bool selected, bool enabled = true)
        {
            Selected = selected;
            Enabled = enabled;

            Name = assembly.GetName().Name;
            Path = assembly.Location;
        }
    }
    public class FlowReference
    {
        public bool Selected { get; set; }

        public string Path { get; private set; }
        public string Name
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            }
        }

        public FlowReference(string flowPath, bool selected)
        {
            Path = flowPath;
            Selected = selected;
        }
    }

    public partial class ReferencesWindow : Window, INotifyPropertyChanged
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const uint WM_SETICON = 0x0080;

        public List<AssemblyInfo> Assemblies { get; } = new List<AssemblyInfo>();
        public ObservableCollection<FlowReference> Flows { get; } = new ObservableCollection<FlowReference>();

        private BackgroundWorker assembliesLoader = new BackgroundWorker();
        private static Type reflectionNodeType, reflectionFlowType;

        static ReferencesWindow()
        {
            Assembly reflectionAssembly = Assembly.ReflectionOnlyLoad(typeof(Node).Assembly.FullName);
            reflectionNodeType = reflectionAssembly.GetType(typeof(Node).FullName);
            reflectionFlowType = reflectionAssembly.GetType(typeof(Flow).FullName);
        }
        public ReferencesWindow()
        {
            InitializeComponent();
            DataContext = this;

            AssembliesGrid.Visibility = Visibility.Collapsed;
            AssembliesProgressBar.Visibility = Visibility.Visible;

            assembliesLoader.DoWork += AssembliesLoader_DoWork;
            assembliesLoader.RunWorkerCompleted += AssembliesLoader_RunWorkerCompleted;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);

            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            SendMessage(hwnd, WM_SETICON, new IntPtr(1), IntPtr.Zero);
            SendMessage(hwnd, WM_SETICON, IntPtr.Zero, IntPtr.Zero);
        }

        private void AssembliesTab_Loaded(object sender, RoutedEventArgs e)
        {
            assembliesLoader.RunWorkerAsync();
        }
        private void AssembliesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            Assemblies.Clear();

            // Add default assembly
            Assemblies.Add(new AssemblyInfo(typeof(Node).Assembly, true, false));

            // Load domain assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                AnalyzeAssembly(assembly, true);

            // Load local plugins
            foreach (string path in Directory.GetFiles(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FlowTomator.*.dll"))
            {
                FileInfo fileInfo = new FileInfo(path);

                if (!fileInfo.Exists || Assemblies.Any(a => a.Path == fileInfo.FullName))
                    continue;

                try
                {
                    Assembly assembly = Assembly.ReflectionOnlyLoadFrom(path);
                    AnalyzeAssembly(assembly, false);
                }
                catch
                {
                    continue;
                }
            }
        }
        private void AssembliesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AssembliesGrid.Visibility = Visibility.Visible;
            AssembliesProgressBar.Visibility = Visibility.Collapsed;

            NotifyPropertyChanged(nameof(Assemblies));
        }
        private void AnalyzeAssembly(Assembly assembly, bool enabled)
        {
            if (assembly.IsDynamic || Assemblies.Any(a => a.Path == assembly.Location))
                return;

            if (assembly.ReflectionOnly)
            {
                foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
                {
                    try
                    {
                        Assembly.ReflectionOnlyLoad(assemblyName.FullName);
                    }
                    catch (FileNotFoundException) { }
                }
            }

            IEnumerable<Type> nodeTypes;

            try
            {
                nodeTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                nodeTypes = e.Types.Where(t => t != null);
            }

            nodeTypes = nodeTypes.Where(t => !t.IsAbstract)
                                 .Where(t => t.IsSubclassOf(typeof(Node)) || t.IsSubclassOf(reflectionNodeType))
                                 .Where(t => t != typeof(Flow) && t != reflectionFlowType)
                                 .Where(t => !t.IsSubclassOf(typeof(Flow)) && !t.IsSubclassOf(reflectionFlowType))
                                 .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

            if (nodeTypes.Any())
                Assemblies.Add(new AssemblyInfo(assembly, enabled));
        }

        private void FlowTab_Loaded(object sender, RoutedEventArgs e)
        {
            Flows.Clear();

            // Load default flows
            if (Settings.Default.Flows != null)
            {
                foreach (string path in Settings.Default.Flows)
                {
                    string flowPath = path;

                    if (!System.IO.Path.IsPathRooted(flowPath))
                        flowPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), flowPath);

                    Flows.Add(new FlowReference(flowPath, true));
                }
            }

            // Load local flows
            foreach (string path in Directory.GetFiles(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.xflow"))
            {
                FileInfo fileInfo = new FileInfo(path);

                if (!fileInfo.Exists || Flows.Any(f => f.Path == fileInfo.FullName))
                    continue;

                Flows.Add(new FlowReference(fileInfo.FullName, true));
            }

            NotifyPropertyChanged(nameof(Flows));
        }

        private void AddAssemblyButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "Open an existing .NET assembly";
            dialog.Filter = ".NET Assemblies|*.dll|All files|*.*";
            dialog.FilterIndex = 1;

            if (dialog.ShowDialog(this) != true)
                return;

            FileInfo fileInfo = new FileInfo(dialog.FileName);

            if (!fileInfo.Exists)
            {
                MessageBox.Show("Unable to find the specified file", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                    
            if (Assemblies.Any(a => a.Path == fileInfo.FullName))
            {
                MessageBox.Show("The specified file has already been added", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Assembly assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                AnalyzeAssembly(assembly, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load the specified assembly. " + ex, App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        private void AddFlowButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "Open an existing Flow";
            dialog.Filter = "FlowTomator flow file|*.xflow";
            dialog.FilterIndex = 1;

            if (dialog.ShowDialog(this) != true)
                return;

            FileInfo fileInfo = new FileInfo(dialog.FileName);

            if (!fileInfo.Exists)
            {
                MessageBox.Show("Unable to find the specified file", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Flows.Any(f => f.Path == fileInfo.FullName))
            {
                MessageBox.Show("The specified file has already been added", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Flows.Add(new FlowReference(fileInfo.FullName, false));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load the specified flow. " + ex, App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            NotifyPropertyChanged(nameof(Flows));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            AssemblyInfo[] assemblies = Assemblies.Where(a => a.Selected && a.Enabled).ToArray();
            FlowReference[] flows = Flows.Where(f => f.Selected).ToArray();

            // Load selected assemblies
            foreach (AssemblyInfo assembly in assemblies)
            {
                try
                {
                    Assembly.LoadFrom(assembly.Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to load the specified assembly. " + ex, App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Save selected assemblies
            if (Settings.Default.Assemblies == null)
                Settings.Default.Assemblies = new StringCollection();

            Settings.Default.Assemblies.Clear();
            string executablePath = Assembly.GetExecutingAssembly().Location;
            foreach (AssemblyInfo assembly in assemblies)
            {
                string assemblyPath = assembly.Path;
                
                try
                {
                    assemblyPath = Utilities.MakeRelativePath(executablePath, assemblyPath);
                }
                catch { }
                
                Settings.Default.Assemblies.Add(assemblyPath);
            }

            // Save selected flows
            if (Settings.Default.Flows == null)
                Settings.Default.Flows = new StringCollection();

            Settings.Default.Flows.Clear();
            foreach (FlowReference flow in flows)
            {
                string flowPath = flow.Path;

                try
                {
                    flowPath = Utilities.MakeRelativePath(executablePath, flowPath);
                }
                catch { }

                Settings.Default.Flows.Add(flowPath);
            }

            // Save settings
            Settings.Default.Save();

            Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}