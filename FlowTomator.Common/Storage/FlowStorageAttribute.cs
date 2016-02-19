using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class FlowStorageAttribute : Attribute
    {
        public string Description { get; set; }
        public string[] Extensions { get; set; }

        public FlowStorageAttribute(string description, params string[] extensions)
        {
            Description = description;
            Extensions = extensions;
        }
    }
}