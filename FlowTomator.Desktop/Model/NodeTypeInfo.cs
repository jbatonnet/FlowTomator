using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class NodeTypeInfo
    {
        private static Dictionary<Type, NodeTypeInfo> typeInfos = new Dictionary<Type, NodeTypeInfo>();

        private static Color[] colors = new[] { Colors.DeepSkyBlue, Colors.Tomato, Colors.MediumSeaGreen, Colors.SandyBrown, Colors.LightSeaGreen };
        private static int colorIndex = 0;

        private static Dictionary<string, Color> categoryColors = new Dictionary<string, Color>();

        public Type Type { get; private set; }

        public NodeAttribute Attribute
        {
            get
            {
                return Type.GetCustomAttribute<NodeAttribute>();
            }
        }
        public string Name
        {
            get
            {
                return Attribute?.Name ?? Type.Name;
            }
        }
        public string Description
        {
            get
            {
                return Attribute?.Description;
            }
        }
        public string Category
        {
            get
            {
                return Attribute?.Category ?? "Other";
            }
        }
        public string Model
        {
            get
            {
                if (Type.IsSubclassOf(typeof(Flow)))
                    return nameof(Flow);
                if (Type.IsSubclassOf(typeof(Choice)))
                    return nameof(Choice);
                if (Type.IsSubclassOf(typeof(Event)))
                    return nameof(Event);
                if (Type.IsSubclassOf(typeof(Task)))
                    return nameof(Task);

                return nameof(Node);
            }
        }
        public Color Color
        {
            get
            {
                Color color;

                if (!categoryColors.TryGetValue(Category, out color))
                {
                    categoryColors.Add(Category, color = colors[colorIndex++]);
                    colorIndex %= colors.Length;
                }

                return color;
            }
        }

        static NodeTypeInfo()
        {
            categoryColors.Add("General", colors[colorIndex++]);
            categoryColors.Add("Other", colors[colorIndex++]);
        }
        private NodeTypeInfo(Type type)
        {
            Type = type;
        }

        public static NodeTypeInfo From(Type type)
        {
            if (type != typeof(Node) && !type.IsSubclassOf(typeof(Node)))
                return null;

            NodeTypeInfo typeInfo;

            if (!typeInfos.TryGetValue(type, out typeInfo))
                typeInfos.Add(type, typeInfo = new NodeTypeInfo(type));

            return typeInfo;
        }
    }
}