using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlowTomator.Desktop
{
    public partial class References : Window
    {
        public ObservableDictionary<Assembly, bool> Assemblies { get; } = new ObservableDictionary<Assembly, bool>();
        public ObservableDictionary<FileInfo, bool> Flows { get; } = new ObservableDictionary<FileInfo, bool>();

        public References()
        {
            InitializeComponent();

            // Load assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                AnalyzeAssembly(assembly);

        }

        private void AnalyzeAssembly(Assembly assembly)
        {
            /*IEnumerable<Type> nodeTypes = Enumerable.Empty<Type>();

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
            }*/
        }
    }
}