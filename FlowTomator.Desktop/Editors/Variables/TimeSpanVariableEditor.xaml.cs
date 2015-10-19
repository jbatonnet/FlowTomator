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

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    [VariableEditor(typeof(TimeSpan))]
    public partial class TimeSpanVariableEditor : UserControl
    {
        public VariableInfo VariableInfo
        {
            get
            {
                return DataContext as VariableInfo;
            }
        }
        public Variable<TimeSpan> Variable
        {
            get
            {
                return VariableInfo.Variable as Variable<TimeSpan>;
            }
        }

        public string Text
        {
            get
            {
                if (Variable.Value == TimeSpan.MinValue)
                    return "MinValue";
                else if (Variable.Value == TimeSpan.MaxValue)
                    return "MaxValue";

                return Variable.Value.ToString();
            }
            set
            {
                
            }
        }

        public TimeSpanVariableEditor()
        {
            InitializeComponent();
        }
    }
}