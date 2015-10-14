using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public abstract class Action
    {
        public abstract void Do();
        public abstract void Undo();
    }
}