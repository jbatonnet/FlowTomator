using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public partial class DynamicEnumerableVariableEditor : UserControl
    {
        public VariableInfo VariableInfo { get; private set; }
        public Variable Variable
        {
            get
            {
                return VariableInfo.Variable;
            }
        }
        public Type VariableType
        {
            get
            {
                return Variable.GetType();
            }
        }

        public object Value
        {
            get
            {
                return VariableInfo.Value.ToString();
            }
            set
            {
                PropertyInfo property = VariableType.GetProperty("Values");
                IEnumerable values = property.GetValue(Variable) as IEnumerable;

                VariableInfo.Value = values.OfType<object>().FirstOrDefault(v => v.ToString() == value.ToString());
            }
        }
        public string[] Values
        {
            get
            {
                PropertyInfo property = VariableType.GetProperty("Values");
                IEnumerable values = property.GetValue(Variable) as IEnumerable;

                return values.OfType<object>().Select(v => v.ToString()).ToArray();
            }
        }

        public DynamicEnumerableVariableEditor()
        {
            InitializeComponent();

            DataContextChanged += DynamicEnumerableVariableEditor_DataContextChanged;
        }

        private void DynamicEnumerableVariableEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is VariableInfo)
                VariableInfo = e.NewValue as VariableInfo;
        }
    }
}