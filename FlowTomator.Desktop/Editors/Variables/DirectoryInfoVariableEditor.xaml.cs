using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using FlowTomator.Common;

using Microsoft.Win32;

namespace FlowTomator.Desktop
{
    [VariableEditor(typeof(DirectoryInfo))]
    public partial class DirectoryInfoVariableEditor : System.Windows.Controls.UserControl
    {
        public VariableInfo VariableInfo
        {
            get
            {
                return DataContext as VariableInfo;
            }
        }

        public DirectoryInfoVariableEditor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();

            dialog.Title = (VariableInfo.Value as DirectoryInfo)?.FullName;

            if (!dialog.ShowDialog())
                return;

            VariableInfo.Value = new DirectoryInfo(dialog.FileName);
        }
    }
}