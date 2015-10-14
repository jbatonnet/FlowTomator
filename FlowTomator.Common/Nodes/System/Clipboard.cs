using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public class SetClipboard : Task
    {
        public override NodeResult Run()
        {
            return NodeResult.Success;
        }
    }

    public class GetClipboard : Task
    {
        public override NodeResult Run()
        {
            return NodeResult.Success;
        }
    }
}