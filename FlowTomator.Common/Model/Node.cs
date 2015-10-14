using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public enum NodeResult
    {
        Success,
        Skip,
        Stop,
        Fail
    }
    public class NodeStep
    {
        public NodeResult Result { get; private set; }
        public Slot Slot { get; private set; }

        public NodeStep(NodeResult result, Slot slot)
        {
            Result = result;
            Slot = slot;
        }
    }

    public abstract class Node
    {
        public virtual IEnumerable<Variable> Inputs { get; } = Enumerable.Empty<Variable>();
        public virtual IEnumerable<Variable> Outputs { get; } = Enumerable.Empty<Variable>();
        public virtual IEnumerable<Slot> Slots { get; } = Enumerable.Empty<Slot>();

        public virtual Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public abstract NodeStep Evaluate();
    }
}