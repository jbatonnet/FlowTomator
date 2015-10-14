using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public abstract class Event : Node
    {
        public sealed override IEnumerable<Slot> Slots
        {
            get
            {
                return new[] { slot };
            }
        }
        public TimeSpan Timeout { get; set; } = TimeSpan.MaxValue;

        private Slot slot = new Slot("Callbacks");

        public abstract NodeResult Check();
        public sealed override NodeStep Evaluate()
        {
            return new NodeStep(Check(), slot);
        }
    }
}