using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public abstract class NodesEvaluator
    {
        public abstract IList<Node> Nodes { get; }
        public abstract bool Evaluating { get; }

        public abstract NodeResult Evaluate();
        public abstract void BeginEvaluate();
        public abstract NodeResult EndEvaluate();

        public abstract void Stop();
    }
}