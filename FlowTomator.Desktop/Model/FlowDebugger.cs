using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    public enum DebuggerState
    {
        Idle,
        Break,
        Running,
    }

    public class FlowDebugger : DependencyModel
    {
        public FlowInfo FlowInfo { get; private set; }
        public DebuggerState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                NotifyPropertyChanged();
            }
        }

        private DebuggerState state = DebuggerState.Idle;
        private List<NodeInfo> nodes = new List<NodeInfo>();
        private List<Task> tasks = new List<Task>();

        public FlowDebugger(FlowInfo flowInfo)
        {
            FlowInfo = flowInfo;
        }

        public void Run()
        {
            if (State == DebuggerState.Idle)
                Reset();

            if (State == DebuggerState.Break)
            {
                State = DebuggerState.Running;
                Task.Run(Evaluate);
            }
        }
        public void Step()
        {
            if (State == DebuggerState.Idle)
                Reset();
            else if (State == DebuggerState.Break)
                Task.Run(Evaluate);
        }
        public void Break()
        {
            if (State == DebuggerState.Running)
                State = DebuggerState.Break;
        }
        public void Stop()
        {
            if (State == DebuggerState.Idle)
                return;

            State = DebuggerState.Idle;

            nodes.Clear();
            tasks.Clear();

            foreach (NodeInfo nodeInfo in FlowInfo.Flow.GetAllNodes().Select(n => NodeInfo.From(FlowInfo, n)))
                nodeInfo.Status = NodeStatus.Idle;
        }

        private void Reset()
        {
            FlowInfo.Flow.Reset();

            nodes.Clear();
            nodes = FlowInfo.Flow.Origins
                                 .Select(n => NodeInfo.From(FlowInfo, n))
                                 .ToList();

            foreach (NodeInfo nodeInfo in nodes)
                nodeInfo.Status = NodeStatus.Paused;

            State = DebuggerState.Break;
        }
        private async Task Evaluate()
        {
            NodeInfo[] stepNodes;

            lock (nodes)
            {
                if (nodes.Count == 0)
                    return;

                stepNodes = nodes.ToArray();
                nodes.Clear();
            }

            foreach (NodeInfo nodeInfo in stepNodes)
            {
                Task task = Task.Run(() => Evaluate(nodeInfo));
                //Task task = Evaluate(nodeInfo);

                lock (tasks)
                    tasks.Add(task);

                task.ContinueWith(t =>
                {
                    lock (tasks)
                    {
                        tasks.Remove(t);

                        if (tasks.Count == 0 && nodes.Count == 0)
                            State = DebuggerState.Idle;
                    }

                    if (State == DebuggerState.Running)
                        Task.Run(Evaluate);
                });
            }
        }
        private async Task Evaluate(NodeInfo nodeInfo)
        {
            NodeStep nodeStep;
            nodeInfo.Status = NodeStatus.Running;
           
            try
            {
                nodeStep = nodeInfo.Node.Evaluate();
            }
            catch
            {
                nodeStep = new NodeStep(NodeResult.Fail, null);
            }

            if (State == DebuggerState.Idle)
                return;

            nodeInfo.Status = NodeStatus.Idle;
            nodeInfo.Result = nodeStep.Result;

            switch (nodeStep.Result)
            {
                case NodeResult.Skip: return;
                case NodeResult.Fail: Break(); return;
                case NodeResult.Stop: Stop(); return;
            }

            NodeInfo[] nodeInfos = nodeStep.Slot.Nodes.Select(n => NodeInfo.From(FlowInfo, n)).ToArray();

            foreach (NodeInfo node in nodeInfos)
                node.Status = NodeStatus.Paused;

            lock (nodes)
                nodes.AddRange(nodeInfos);
        }
    }
}