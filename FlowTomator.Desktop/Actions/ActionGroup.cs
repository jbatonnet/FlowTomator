using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class ActionGroup : Action
    {
        public Action[] Actions { get; private set; }

        public ActionGroup(params Action[] actions)
        {
            Actions = actions;
        }
        public ActionGroup(IEnumerable<Action> actions)
        {
            Actions = actions.ToArray();
        }

        public override void Do()
        {
            foreach (Action action in Actions)
                action.Do();
        }

        public override void Undo()
        {
            foreach (Action action in Actions.Reverse())
                action.Undo();
        }
    }
}