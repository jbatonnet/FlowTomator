using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class EditableFlow : Flow
    {
        public override IEnumerable<Origin> Origins
        {
            get
            {
                return Nodes.OfType<Origin>();
            }
        }
        public virtual IList<Node> Nodes { get; } = new List<Node>();

        public override IEnumerable<Node> GetAllNodes()
        {
            return Nodes;
        }
    }
}