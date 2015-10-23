using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class EditVariableAction : Action
    {
        public VariableInfo VariableInfo { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }

        public EditVariableAction(VariableInfo variableInfo, object oldValue, object newValue)
        {
            VariableInfo = variableInfo;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override void Do()
        {
            VariableInfo.Variable.Value = NewValue;
            VariableInfo.Update();
        }

        public override void Undo()
        {
            VariableInfo.Variable.Value = OldValue;
            VariableInfo.Update();
        }
    }
}