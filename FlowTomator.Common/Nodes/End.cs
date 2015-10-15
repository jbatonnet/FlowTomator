using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("End", "General", "Exits this flow with the specified result")]
    public class End : Node
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return result;
            }
        }

        private Variable<NodeResult> result = new Variable<NodeResult>("Result", NodeResult.Success, "The result to return");

        public override NodeStep Evaluate()
        {
            return new NodeStep(result.Value, null);
        }
    }
}