using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("Debug", "General", "Returns the specified result flag")]
    public class Debug : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return result;
            }
        }

        private Variable<NodeResult> result = new Variable<NodeResult>("Result", NodeResult.Success, "The result flag to return");

        public override NodeResult Run()
        {
            return result.Value;
        }
    }
}