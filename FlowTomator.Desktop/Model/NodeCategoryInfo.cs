using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class NodeCategoryInfo
    {
        private static Brush[] colors = new[] { Brushes.CornflowerBlue, Brushes.Firebrick, Brushes.ForestGreen, Brushes.SandyBrown };
        private static int colorIndex = 0;

        private static Dictionary<string, Brush> categoryColors = new Dictionary<string, Brush>();

        public string Category { get; private set; }
        public ObservableCollection<NodeTypeInfo> Nodes { get; } = new ObservableCollection<NodeTypeInfo>();

        public Brush Color
        {
            get
            {
                Brush color;

                if (!categoryColors.TryGetValue(Category, out color))
                {
                    categoryColors.Add(Category, color = colors[colorIndex++]);
                    colorIndex %= colors.Length;
                }

                return color;
            }
        }

        public NodeCategoryInfo(string category)
        {
            Category = category;
        }
    }
}