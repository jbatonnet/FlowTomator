using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public abstract class Event : Node
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return timeout;
            }
        }
        public sealed override IEnumerable<Slot> Slots
        {
            get
            {
                return new[] { slot };
            }
        }

        protected Variable<TimeSpan> timeout = new Variable<TimeSpan>("Timeout", TimeSpan.MaxValue, "The timeout after this event will be skipped");
        private Slot slot = new Slot("Callbacks");

        public abstract NodeResult Check();
        public sealed override NodeStep Evaluate()
        {
            return new NodeStep(Check(), slot);
        }
    }
}