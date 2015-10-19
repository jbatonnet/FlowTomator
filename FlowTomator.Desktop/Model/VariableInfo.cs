using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    public class VariableInfo : INotifyPropertyChanged
    {
        private static Dictionary<Variable, VariableInfo> variableInfos = new Dictionary<Variable, VariableInfo>();

        public Variable Variable { get; private set; }
        public Type Type
        {
            get
            {
                return Variable.Type;
            }
        }
        public object Value
        {
            get
            {
                return Variable.Value;
            }
            set
            {
                Variable.Value = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Value)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Text)));
                }
            }
        }
        public string Text
        {
            get
            {
                if (Variable.Linked != null)
                    return "$" + Variable.Linked.Name;
                else if (Variable.Value == null)
                    return "null";
                else if (Variable.Value is TimeSpan)
                    return (TimeSpan)Variable.Value == TimeSpan.MinValue ? "MinValue" : (TimeSpan)Variable.Value == TimeSpan.MaxValue ? "MaxValue" : Variable.Value.ToString();
                else if (Variable.Value is DateTime)
                    return (DateTime)Variable.Value == DateTime.MinValue ? "MinValue" : (DateTime)Variable.Value == DateTime.MaxValue ? "MaxValue" : Variable.Value.ToString();
                else
                    return Variable.Value.ToString();
            }
        }

        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(DisplayVisibility)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(EditorVisibility)));
                }
            }
        }
        public Visibility DisplayVisibility
        {
            get
            {
                return Selected ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        public Visibility EditorVisibility
        {
            get
            {
                return Selected ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool selected = false;

        private VariableInfo(Variable variable)
        {
            Variable = variable;
        }

        public static VariableInfo From(Variable variable)
        {
            VariableInfo variableInfo;

            if (!variableInfos.TryGetValue(variable, out variableInfo))
                variableInfos.Add(variable, variableInfo = new VariableInfo(variable));

            return variableInfo;
        }
    }
}