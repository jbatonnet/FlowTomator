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
            Log.Trace("Entering task {0}", GetType().Name);

            NodeStep step = new NodeStep(Run(), slot);

            Log.Trace("Exiting task {0} with result {1}", GetType().Name, step.Result);
            return step;
        }
    }
}