using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class EditorAttribute : Attribute
    {
        public Type[] Types { get; private set; }

        public EditorAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}