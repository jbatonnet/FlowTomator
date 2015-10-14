using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("End", "General", "Exits this flow")]
    public class End : Node
    {
        public override NodeStep Evaluate()
        {
            return new NodeStep(NodeResult.Stop, null);
        }
    }
}