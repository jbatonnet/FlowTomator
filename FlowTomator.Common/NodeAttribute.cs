using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class NodeAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Category { get; private set; }
        public string Description { get; private set; }

        public NodeAttribute(string name, string category, string description)
        {
            Name = name;
            Category = category;
            Description = description;
        }
    }
}