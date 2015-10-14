using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    using Task = System.Threading.Tasks.Task;

    public enum DebuggerState
    {
        Idle,
        Running,
        Break
    }

    public class FlowDebugger
    {
        public FlowInfo FlowInfo { get; private set; }
        public DebuggerState State { get; private set; } = DebuggerState.Idle;

        private List<NodeInfo> nodes = new List<NodeInfo>();

        public FlowDebugger(FlowInfo flowInfo)
        {
            FlowInfo = flowInfo;
        }

        public void Run()
        {
            if (State == DebuggerState.Idle)
                Reset();


        }

        private void Reset()
        {
            nodes.Clear();
            nodes = FlowInfo.Flow.Origins
                                 .Select(n => NodeInfo.From(FlowInfo, n))
                                 .ToList();

            State = DebuggerState.Idle;
        }

        private void Step()
        {
            if (State == DebuggerState.Break)
                Evaluate();
        }

        private Task<NodeStep[]> Evaluate()
        {
            return Task.WhenAll(nodes.Select(n => Evaluate(n)));
        }
        private async Task<NodeStep> Evaluate(NodeInfo nodeInfo)
        {
            return nodeInfo.Node.Evaluate();
        }
    }
}