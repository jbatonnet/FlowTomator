using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class LinkVariableAction : Action
    {
        public VariableInfo VariableInfo { get; private set; }
        public Variable OldLink { get; private set; }
        public object OldValue { get; private set; }
        public Variable NewLink { get; private set; }

        public LinkVariableAction(VariableInfo variableInfo, Variable linkedVariable)
        {
            VariableInfo = variableInfo;
            OldLink = variableInfo.Variable.Linked;
            OldValue = variableInfo.Value;
            NewLink = linkedVariable;
        }

        public override void Do()
        {
            VariableInfo.Variable.Link(NewLink);
            VariableInfo.Update();
        }

        public override void Undo()
        {
            VariableInfo.Variable.Link(OldLink);
            VariableInfo.Variable.Value = OldValue;

            VariableInfo.Update();
        }
    }
}