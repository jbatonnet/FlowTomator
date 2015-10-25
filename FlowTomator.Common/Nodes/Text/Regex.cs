using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("MatchRegex", "Text", "Test the specified value against the specified pattern")]
    public class MatchRegex : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return value;
                yield return pattern;
            }
        }

        private Variable<string> value = new Variable<string>("Value");
        private Variable<string> pattern = new Variable<string>("Pattern");

        public override NodeStep Evaluate()
        {
            try
            {
                bool result = Regex.IsMatch(value.Value, pattern.Value);
                return new NodeStep(NodeResult.Success, result ? TrueSlot : FalseSlot);
            }
            catch
            {
                return new NodeStep(NodeResult.Fail);
            }
        }
    }
}