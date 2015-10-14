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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using FlowTomator.Common;
using Microsoft.Win32;

namespace FlowTomator.Desktop
{
    public partial class FileInfoEditor : UserControl
    {
        public VariableInfo VariableInfo
        {
            get
            {
                return DataContext as VariableInfo;
            }
        }

        public FileInfoEditor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckFileExists = false;

            if (fileDialog.ShowDialog() != true)
                return;

            VariableInfo.Value = new FileInfo(fileDialog.FileName);
        }
    }
}