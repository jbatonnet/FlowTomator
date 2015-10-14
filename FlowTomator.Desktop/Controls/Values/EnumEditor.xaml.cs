using System;
using System.Collections.Generic;
using System.Linq;
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

namespace FlowTomator.Desktop
{
    public partial class EnumEditor : UserControl
    {
        public object Value
        {
            get
            {
                return variableInfo.Value.ToString();
            }
            set
            {
                variableInfo.Value = Enum.Parse(variableInfo.Type, (string)value);
            }
        }
        public string[] Values
        {
            get
            {
                return Enum.GetNames(variableInfo.Type);
            }
        }

        private VariableInfo variableInfo;

        public EnumEditor()
        {
            InitializeComponent();
            DataContextChanged += EnumEditor_DataContextChanged;
        }

        private void EnumEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext == this)
                return;

            variableInfo = e.NewValue as VariableInfo;
        }
    }
}