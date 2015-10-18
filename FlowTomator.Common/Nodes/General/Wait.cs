using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("Wait", "General", "Wait for the specified count of input flows to succeed")]
    public class Wait : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return count;
            }
        }

        private Variable<uint> count = new Variable<uint>("Count", 2, "Number of input flows to wait before resuming");

        private object mutex = new object();
        private uint actualCount = 0;

        public override void Reset()
        {
            base.Reset();

            actualCount = 0;
        }
        public override NodeResult Run()
        {
            lock (mutex)
            {
                actualCount++;

                if (actualCount < count.Value)
                    return NodeResult.Skip;

                actualCount = 0;
                return NodeResult.Success;
            }
        }
    }
}