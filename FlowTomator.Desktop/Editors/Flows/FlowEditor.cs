using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public abstract class FlowEditor : UserControl, INotifyPropertyChanged
    {
        public abstract FlowInfo FlowInfo { get; }
        public abstract ObservableCollection<NodeInfo> SelectedNodes { get; }

        [DependsOn(nameof(SelectedNodes))]
        public virtual ICommand CutCommand
        {
            get
            {
                return disabledCommand;
            }
        }
        [DependsOn(nameof(SelectedNodes))]
        public virtual ICommand CopyCommand
        {
            get
            {
                return disabledCommand;
            }
        }
        [DependsOn(nameof(SelectedNodes))]
        public virtual ICommand PasteCommand
        {
            get
            {
                return disabledCommand;
            }
        }

        private DependencyManager dependencyManager;
        private DelegateCommand disabledCommand = new DelegateCommand(p => { }, p => false);

        public FlowEditor()
        {
            dependencyManager = new DependencyManager(this, (s, e) => NotifyPropertyChanged(e.PropertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property != null && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}