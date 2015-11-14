using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class Flow : Task
    {
        public virtual IEnumerable<Origin> Origins { get; } = new List<Origin>();

        public override void Reset()
        {
            base.Reset();

            foreach (Node node in GetAllNodes())
                node.Reset();
        }
        public override NodeResult Run()
        {
            if (!Origins.Any())
                return NodeResult.Success;

            return Origins.AsParallel()
                          .Select(n => Evaluate(n))
                          .Max();
        }

        private NodeResult Evaluate(Node node)
        {
            NodeStep step = node.Evaluate();

            if (step.Result == NodeResult.Fail)
            {
                Log.Error("Node {0} failed", node.GetType().Name);
                return NodeResult.Fail;
            }
            if (step.Result == NodeResult.Skip || step.Slot == null || step.Slot.Nodes.Count == 0)
                return step.Result;


            //System.Threading.Tasks.Task.WhenAll()


            NodeResult result = NodeResult.Success;
            object resultMutex = new object();

            Semaphore semaphore = new Semaphore(step.Slot.Nodes.Count, 1);
                      

            semaphore.WaitOne();

            Parallel.ForEach(step.Slot.Nodes, new ParallelOptions() { MaxDegreeOfParallelism = step.Slot.Nodes.Count }, n =>
            {
                NodeResult nodeResult = Evaluate(n);

                lock (resultMutex)
                    if (nodeResult > result)
                        result = nodeResult;
            });

            return result;
        }

        public virtual IEnumerable<Node> GetAllNodes()
        {
            List<Node> knownNodes = new List<Node>();
            return Origins.SelectMany(n => GetAllNodes(n, knownNodes)).Distinct();
        }
        private IEnumerable<Node> GetAllNodes(Node node, List<Node> knownNodes)
        {
            if (knownNodes.Contains(node))
                yield break;

            yield return node;
            knownNodes.Add(node);

            foreach (Slot slot in node.Slots)
                foreach (Node slotNode in slot.Nodes)
                    foreach (Node subNode in GetAllNodes(slotNode, knownNodes))
                    {
                        if (knownNodes.Contains(subNode))
                            continue;

                        yield return subNode;
                        knownNodes.Add(subNode);
                    }
        }
    }
}