using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public enum NodeResult
    {
        /// <summary>
        /// Return value if the node evaluation has succeeded
        /// </summary>
        Success,

        /// <summary>
        /// Return value if the runtime should skip further thread evaluation
        /// </summary>
        Skip,

        /// <summary>
        /// Return value if the flow should stop its evaluation
        /// </summary>
        Stop,

        /// <summary>
        /// Return value if the node evaluation has failed
        /// </summary>
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

        public virtual void Reset() { }
        public abstract NodeStep Evaluate();
    }
}