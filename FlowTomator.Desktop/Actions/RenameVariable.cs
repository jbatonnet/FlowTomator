using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class RenameVariableAction : Action
    {
        public VariableInfo VariableInfo { get; private set; }

        public RenameVariableAction(VariableInfo variableInfo, string name)
        {
            VariableInfo = variableInfo;
        }

        public override void Do()
        {
            VariableInfo.Update();
        }

        public override void Undo()
        {
            VariableInfo.Update();
        }
    }
}