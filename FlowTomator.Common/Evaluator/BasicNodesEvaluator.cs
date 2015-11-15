using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class BasicNodesEvaluator : NodesEvaluator
    {
        public override IList<Node> Nodes
        {
            get
            {
                return nodes;
            }
        }
        public override bool Evaluating
        {
            get
            {
                return evaluating;
            }
        }

        private List<Node> nodes = new List<Node>();
        private List<Thread> threads = new List<Thread>();
        private ManualResetEvent end = new ManualResetEvent(false);
        private NodeResult result = NodeResult.Success;

        private bool evaluating = false;
        private bool evaluated = false;

        public override NodeResult Evaluate()
        {
            BeginEvaluate();
            return EndEvaluate();
        }
        public override void BeginEvaluate()
        {
            if (evaluating)
                throw new Exception("This evaluator is already being evaluated");

            evaluating = true;
            evaluated = false;
            threads.Clear();

            EvaluateNodes();
        }
        public override NodeResult EndEvaluate()
        {
            if (!evaluating)
                throw new Exception("This evaluator's evaluation has not been started");

            end.WaitOne();

            return result;
        }

        public override void Stop()
        {
            Return(NodeResult.Stop);
        }

        protected virtual void EvaluateNodes()
        {
            Node[] stepNodes;

            lock (Nodes)
            {
                if (Nodes.Count == 0)
                    return;

                stepNodes = Nodes.ToArray();
                Nodes.Clear();
            }

            Thread[] threads = new Thread[stepNodes.Length];

            for (int i = 0; i < stepNodes.Length; i++)
            {
                Node node = stepNodes[i];

                // Create the thread
                threads[i] = new Thread(() =>
                {
                    NodeStep step = EvaluateNode(node);

                    if (!evaluating)
                    {
                        Return(NodeResult.Stop);
                        return;
                    }

                    switch (step.Result)
                    {
                        case NodeResult.Fail: Return(NodeResult.Fail); return;
                        case NodeResult.Stop: Return(NodeResult.Stop); return;
                    }

                    if (step.Result == NodeResult.Success)
                    {
                        lock (Nodes)
                            foreach (Node slotNode in step.Slot.Nodes)
                                Nodes.Add(slotNode);
                    }

                    lock (this.threads)
                    {
                        this.threads.Remove(Thread.CurrentThread);

                        if (this.threads.Count == 0 && Nodes.Count == 0)
                        {
                            Return();
                            return;
                        }
                    }

                    if (!evaluated)
                        EvaluateNodes();
                });
            }

            // Register threads
            lock (this.threads)
                this.threads.AddRange(threads);

            // Start threads
            foreach (Thread thread in threads)
                thread.Start();
        }
        protected virtual NodeStep EvaluateNode(Node node)
        {
            NodeStep step;
            Log.Trace("Entering node {0}", node.GetType().Name);

            try
            {
                step = node.Evaluate();
            }
            catch
            {
                step = new NodeStep(NodeResult.Fail, null);
            }

            Log.Trace("Exiting node {0} with result {1}{2}", node.GetType().Name, step.Result, step.Slot == null ? "" : (" by slot " + step.Slot.Name));
            return step;
        }

        protected void Return(NodeResult result = NodeResult.Success)
        {
            this.result = result;

            evaluating = false;
            evaluated = true;

            end.Set();
        }
    }
}