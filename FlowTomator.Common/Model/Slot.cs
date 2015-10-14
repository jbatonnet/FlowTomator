using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class Slot
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public List<Node> Nodes { get; } = new List<Node>();

        public Slot(string name)
        {
            Name = name;
        }
        public Slot(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}