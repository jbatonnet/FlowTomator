using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class History
    {
        public IEnumerable<Action> Actions
        {
            get
            {
                return actions.AsReadOnly();
            }
        }

        private List<Action> actions = new List<Action>();
        private int actionIndex = 0;
        
        public void Do(Action action)
        {
            while (actions.Count > actionIndex)
                actions.RemoveAt(actionIndex);

            actions.Add(action);
            Redo();
        }

        public void Undo()
        {
            if (actionIndex > 0)
                actions[actionIndex--].Undo();
        }
        public void Redo()
        {
            if (actionIndex < actions.Count)
                actions[actionIndex++].Do();
        }

        public void Clear()
        {
            actions.Clear();
            actionIndex = 0;
        }
    }
}