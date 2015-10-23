using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FlowTomator.Desktop
{
    public class DelegateCommand : ICommand, IPropertyUpdatable
    {
        public Action<object> ExecuteCallback { get; private set; }
        public Func<object, bool> CanExecuteCallback { get; private set; }

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<object> executeCallback)
        {
            ExecuteCallback = executeCallback;
        }
        public DelegateCommand(Action<object> executeCallback, Func<object, bool> canExecuteCallback)
        {
            ExecuteCallback = executeCallback;
            CanExecuteCallback = canExecuteCallback;
        }

        public bool CanExecute(object parameter)
        {
            if (CanExecuteCallback != null)
                return CanExecuteCallback(parameter);

            return true;
        }
        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            if (ExecuteCallback != null)
            {
                if (Application.Current?.Dispatcher != null)
                    Application.Current.Dispatcher.Invoke(() => ExecuteCallback(parameter));
                else if (Dispatcher.CurrentDispatcher != null)
                    Dispatcher.CurrentDispatcher.Invoke(() => ExecuteCallback(parameter));
                else
                    ExecuteCallback(parameter);
            }
        }

        public void PropertyUpdate()
        {
            if (CanExecuteChanged != null)
            {
                if (Application.Current?.Dispatcher != null)
                    Application.Current.Dispatcher.Invoke(() => CanExecuteChanged(this, new EventArgs()));
                else if (Dispatcher.CurrentDispatcher != null)
                    Dispatcher.CurrentDispatcher.Invoke(() => CanExecuteChanged(this, new EventArgs()));
                else
                    CanExecuteChanged(this, new EventArgs());
            }
        }
    }
}