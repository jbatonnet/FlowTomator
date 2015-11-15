using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public abstract class Choice : Node
    {
    }

    public abstract class BinaryChoice : Choice
    {
        public sealed override IEnumerable<Slot> Slots
        {
            get
            {
                return new[] { TrueSlot, FalseSlot };
            }
        }

        protected Slot TrueSlot { get; } = new Slot("True");
        protected Slot FalseSlot { get; } = new Slot("False");
    }
}