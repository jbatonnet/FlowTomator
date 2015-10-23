using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Desktop
{
    public class DependencyModel : DependencyManager, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DependencyModel()
        {
            Instance = this;
            PropertyChangedEventHandler = (o, e) => PropertyChanged(o, e);

            Initialize();
        }

        protected void NotifyPropertyChanged([CallerMemberName]string property = null)
        {
            if (property == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        protected void NotifyPropertyChanged(params string[] properties)
        {
            foreach (string property in properties)
                NotifyPropertyChanged(property);
        }
    }
}