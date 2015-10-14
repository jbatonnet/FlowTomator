using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("Nope", "General", "Does nothing")]
    public class Nope : Task
    {
        public override NodeResult Run()
        {
            return NodeResult.Success;
        }
    }
}