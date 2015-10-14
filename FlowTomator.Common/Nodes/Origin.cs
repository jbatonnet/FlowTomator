using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("Origin", "General", "A new flow origin")]
    public class Origin : Task
    {
        public override NodeResult Run()
        {
            return NodeResult.Success;
        }
    }
}