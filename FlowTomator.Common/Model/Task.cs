using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public abstract class Task : Node
    {
        public sealed override IEnumerable<Slot> Slots
        {
            get
            {
                return new[] { slot };
            }
        }

        private Slot slot = new Slot("Next nodes");

        public abstract NodeResult Run();
        public sealed override NodeStep Evaluate()
        {
            return new NodeStep(Run(), slot);
        }
    }
}