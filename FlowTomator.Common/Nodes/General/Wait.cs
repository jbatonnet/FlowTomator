using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private ManualResetEvent waitEvent = new ManualResetEvent(false);

        public override void Reset()
        {
            base.Reset();

            actualCount = 0;
            waitEvent.Reset();
        }
        public override NodeResult Run()
        {
            uint lastCount;

            lock (mutex)
            {
                actualCount++;
                Log.Debug("[Wait] {0}/{1}", actualCount, count.Value);

                lastCount = actualCount;
            }

            if (lastCount < count.Value)
            {
                waitEvent.WaitOne();
                return NodeResult.Skip;
            }
            else
            {
                lock (mutex)
                    actualCount = 0;

                waitEvent.Set();
                return NodeResult.Success;
            }
        }
    }
}