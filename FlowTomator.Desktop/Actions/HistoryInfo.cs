using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class HistoryInfo : DependencyModel
    {
        public IEnumerable<Action> Actions
        {
            get
            {
                return actions.AsReadOnly();
            }
        }

        [DependsOn(nameof(Actions))]
        public bool CanUndo
        {
            get
            {
                return actionIndex > 0;
            }
        }
        [DependsOn(nameof(Actions))]
        public bool CanRedo
        {
            get
            {
                return actionIndex < actions.Count;
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

            NotifyPropertyChanged(nameof(Actions));
        }

        public void Undo()
        {
            if (actionIndex > 0)
                actions[--actionIndex].Undo();

            NotifyPropertyChanged(nameof(CanUndo), nameof(CanRedo));
        }
        public void Redo()
        {
            if (actionIndex < actions.Count)
                actions[actionIndex++].Do();

            NotifyPropertyChanged(nameof(CanUndo), nameof(CanRedo));
        }

        public void Clear()
        {
            actions.Clear();
            actionIndex = 0;

            NotifyPropertyChanged(nameof(Actions));
        }
    }
}