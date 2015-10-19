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
    [VariableEditor(typeof(FileInfo))]
    public partial class FileInfoVariableEditor : UserControl
    {
        public VariableInfo VariableInfo
        {
            get
            {
                return DataContext as VariableInfo;
            }
        }
        public Variable<FileInfo> Variable
        {
            get
            {
                return VariableInfo.Variable as Variable<FileInfo>;
            }
        }

        public FileInfoVariableEditor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckFileExists = false;
            fileDialog.FileName = Variable.Value?.FullName;
            fileDialog.Title = "Select a file";

            if (fileDialog.ShowDialog() != true)
                return;

            VariableInfo.Value = new FileInfo(fileDialog.FileName);
        }
    }
}