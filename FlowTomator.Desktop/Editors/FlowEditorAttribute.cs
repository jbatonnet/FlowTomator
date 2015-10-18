using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class FlowEditorAttribute : Attribute
    {
        public Type[] Types { get; private set; }

        public FlowEditorAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}