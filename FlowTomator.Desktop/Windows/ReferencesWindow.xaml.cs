using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
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

    public partial class ReferencesWindow : Window
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

        public ObservableCollection<AssemblyInfo> Assemblies { get; } = new ObservableCollection<AssemblyInfo>();
        public ObservableDictionary<FileInfo, bool> Flows { get; } = new ObservableDictionary<FileInfo, bool>();

        public ReferencesWindow()
        {
            InitializeComponent();

            // Add default assembly
            Assemblies.Add(new AssemblyInfo(typeof(Node).Assembly, true, false));

            // Load domain assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                AnalyzeAssembly(assembly, true);

            // Load local plugins
            foreach (string path in Directory.GetFiles(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
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

            DataContext = this;
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

        private void AnalyzeAssembly(Assembly assembly, bool used)
        {
            if (Assemblies.Any(a => a.Path == assembly.Location))
                return;

            IEnumerable<Type> nodeTypes = assembly.GetTypes();

            nodeTypes = nodeTypes.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)) && t != typeof(Flow) && !t.IsSubclassOf(typeof(Flow)))
                                 .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

            if (nodeTypes.Any())
                Assemblies.Add(new AssemblyInfo(assembly, used));
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
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            AssemblyInfo[] assemblies = Assemblies.Where(a => a.Selected && a.Enabled).ToArray();

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

            // Save settings
            if (Settings.Default.Assemblies == null)
                Settings.Default.Assemblies = new StringCollection();
            Settings.Default.Assemblies.Clear();
            Settings.Default.Assemblies.AddRange(assemblies.Select(a => a.Path).ToArray());

            Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}